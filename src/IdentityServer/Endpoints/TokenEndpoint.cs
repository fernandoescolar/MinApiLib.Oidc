namespace IdentityServer.Endpoints;

/// <summary>
/// The token endpoint
/// </summary>
/// <seealso cref="IdentityServer.Hosting.IEndpointHandler" />
internal class TokenEndpoint : IEndpointHandler
{
    private readonly IClientSecretValidator _clientValidator;
    private readonly ITokenRequestValidator _requestValidator;
    private readonly ITokenResponseGenerator _responseGenerator;
    private readonly IEventService _events;
    private readonly IDPoPProofValidator _dpopProofValidator;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenEndpoint" /> class.
    /// </summary>
    /// <param name="clientValidator">The client validator.</param>
    /// <param name="requestValidator">The request validator.</param>
    /// <param name="responseGenerator">The response generator.</param>
    /// <param name="events">The events.</param>
    /// <param name="dpopProofValidator">The DPoP proof validator.</param>
    /// <param name="logger">The logger.</param>
    public TokenEndpoint(
        IClientSecretValidator clientValidator,
        ITokenRequestValidator requestValidator,
        ITokenResponseGenerator responseGenerator,
        IEventService events,
        IDPoPProofValidator dpopProofValidator,
        ILogger<TokenEndpoint> logger)
    {
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _responseGenerator = responseGenerator;
        _events = events;
        _dpopProofValidator = dpopProofValidator;
        _logger = logger;
    }

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingTokenRequest();

        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
        {
            _logger.InvalidHttpRequestForTokenEndpoint();
            return Error(OidcConstants.TokenErrors.InvalidRequest);
        }

        return await ProcessTokenRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessTokenRequestAsync(HttpContext context)
    {
        _logger.StartTokenRequest();

        var clientResult = await _clientValidator.ValidateAsync(context);

        if (clientResult.Client == null)
        {
            return Error(OidcConstants.TokenErrors.InvalidClient);
        }

        // RFC 9449 section 5: a DPoP proof binds the issued tokens to the client's key.
        if (!DPoPRequest.TryGetProof(context, out var proof))
        {
            return Error(OidcConstants.TokenErrors.InvalidDPoPProof, "The DPoP header must not be repeated");
        }

        DPoPProofValidationResult? dpopResult = null;
        if (proof != null)
        {
            dpopResult = await _dpopProofValidator.ValidateAsync(new DPoPProofValidationContext
            {
                ProofToken = proof,
                Method = HttpMethods.Post,
                Url = DPoPRequest.GetRequestUrl(context)
            });

            if (dpopResult.IsError)
            {
                return Error(dpopResult.Error, dpopResult.ErrorDescription);
            }
        }

        var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
        _logger.CallingIntoTokenRequestValidator(_requestValidator.GetType().FullName);
        var requestResult = await _requestValidator.ValidateRequestAsync(form, clientResult, dpopResult);

        if (requestResult.IsError)
        {
            await _events.RaiseAsync(new TokenIssuedFailureEvent(requestResult));
            return Error(requestResult.Error, requestResult.ErrorDescription, requestResult.CustomResponse);
        }

        _logger.CallingIntoTokenRequestResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(requestResult);

        await _events.RaiseAsync(new TokenIssuedSuccessEvent(response, requestResult));
        LogTokens(response, requestResult);

        _logger.TokenRequestSuccess();
        return new TokenResult(response);
    }

    private TokenErrorResult Error(string error, string? errorDescription = null, Dictionary<string, object>? custom = null)
    {
        var response = new TokenErrorResponse
        {
            Error = error,
            ErrorDescription = errorDescription,
            Custom = custom
        };

        return new TokenErrorResult(response);
    }

    private void LogTokens(TokenResponse response, TokenRequestValidationResult requestResult)
    {
        var clientId = $"{requestResult.ValidatedRequest.Client.ClientId} ({requestResult.ValidatedRequest.Client?.ClientName ?? "no name set"})";
        var subjectId = requestResult.ValidatedRequest.Subject?.GetSubjectId() ?? "no subject";

        if (response.IdentityToken != null)
        {
            _logger.IdentityTokenIssued(clientId, subjectId, response.IdentityToken);
        }
        if (response.RefreshToken != null)
        {
            _logger.RefreshTokenIssued(clientId, subjectId, response.RefreshToken);
        }
        if (response.AccessToken != null)
        {
            _logger.AccessTokenIssued(clientId, subjectId, response.AccessToken);
        }
    }
}
