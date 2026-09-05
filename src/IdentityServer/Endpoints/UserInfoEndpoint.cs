namespace IdentityServer.Endpoints;

/// <summary>
/// The userinfo endpoint
/// </summary>
/// <seealso cref="IdentityServer.Hosting.IEndpointHandler" />
internal class UserInfoEndpoint : IEndpointHandler
{
    private readonly BearerTokenUsageValidator _tokenUsageValidator;
    private readonly IUserInfoRequestValidator _requestValidator;
    private readonly IUserInfoResponseGenerator _responseGenerator;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfoEndpoint" /> class.
    /// </summary>
    /// <param name="tokenUsageValidator">The token usage validator.</param>
    /// <param name="requestValidator">The request validator.</param>
    /// <param name="responseGenerator">The response generator.</param>
    /// <param name="logger">The logger.</param>
    public UserInfoEndpoint(
        BearerTokenUsageValidator tokenUsageValidator,
        IUserInfoRequestValidator requestValidator,
        IUserInfoResponseGenerator responseGenerator,
        ILogger<UserInfoEndpoint> logger)
    {
        _tokenUsageValidator = tokenUsageValidator;
        _requestValidator = requestValidator;
        _responseGenerator = responseGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsPost(context.Request.Method))
        {
            _logger.InvalidHttpMethodForUserInfoEndpoint();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        return await ProcessUserInfoRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessUserInfoRequestAsync(HttpContext context)
    {
        _logger.StartUserInfoRequest();

        // userinfo requires an access token on the request
        var tokenUsageResult = await _tokenUsageValidator.ValidateAsync(context);
        if (tokenUsageResult.TokenFound == false)
        {
            _logger.NoAccessTokenFound();
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        // validate the request
        _logger.CallingIntoUserInfoRequestValidator(_requestValidator.GetType().FullName);
        var validationResult = await _requestValidator.ValidateRequestAsync(tokenUsageResult.Token);

        if (validationResult.IsError)
        {
            return Error(validationResult.Error);
        }

        // RFC 9449 section 7: a DPoP-bound token needs the DPoP scheme and a proof of the bound key.
        var dpopError = await DPoPRequest.ValidateProtectedResourceRequestAsync(
            context,
            tokenUsageResult.Token,
            tokenUsageResult.UsageType == BearerTokenUsageType.DPoPAuthorizationHeader,
            validationResult.TokenValidationResult?.Claims);
        if (dpopError != null)
        {
            SecretLog.UserInfoRequestRejected(_logger, dpopError);
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, dpopError);
        }

        // generate response
        _logger.CallingIntoUserInfoResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(validationResult);

        _logger.EndUserInfoRequest();
        return new UserInfoResult(response);
    }

    private static ProtectedResourceErrorResult Error(string error, string? description = null)
        => new(error, description);
}
