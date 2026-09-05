namespace IdentityServer.Endpoints;

/// <summary>
/// The revocation endpoint
/// </summary>
/// <seealso cref="IdentityServer.Hosting.IEndpointHandler" />
internal class TokenRevocationEndpoint : IEndpointHandler
{
    private readonly ILogger _logger;
    private readonly IClientSecretValidator _clientValidator;
    private readonly ITokenRevocationRequestValidator _requestValidator;
    private readonly ITokenRevocationResponseGenerator _responseGenerator;
    private readonly IEventService _events;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevocationEndpoint" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="clientValidator">The client validator.</param>
    /// <param name="requestValidator">The request validator.</param>
    /// <param name="responseGenerator">The response generator.</param>
    /// <param name="events">The events.</param>
    public TokenRevocationEndpoint(ILogger<TokenRevocationEndpoint> logger,
        IClientSecretValidator clientValidator,
        ITokenRevocationRequestValidator requestValidator,
        ITokenRevocationResponseGenerator responseGenerator,
        IEventService events)
    {
        _logger = logger;
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _responseGenerator = responseGenerator;

        _events = events;
    }

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingRevocationRequest();

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            _logger.InvalidHttpMethod();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        if (!context.Request.HasApplicationFormContentType())
        {
            _logger.InvalidMediaType();
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        var response = await ProcessRevocationRequestAsync(context);

        return response;
    }

    private async Task<IEndpointResult> ProcessRevocationRequestAsync(HttpContext context)
    {
        _logger.StartRevocationRequest();

        var clientValidationResult = await _clientValidator.ValidateAsync(context);

        if (clientValidationResult.IsError)
        {
            return new TokenRevocationErrorResult(OidcConstants.TokenErrors.InvalidClient);
        }

        _logger.ClientValidationSuccessful();

        var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();

        _logger.CallingIntoTokenRevocationRequestValidator(_requestValidator.GetType().FullName);
        var requestValidationResult = await _requestValidator.ValidateRequestAsync(form, clientValidationResult.Client);

        if (requestValidationResult.IsError)
        {
            return new TokenRevocationErrorResult(requestValidationResult.Error);
        }

        _logger.CallingIntoTokenRevocationResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(requestValidationResult);

        if (response.Success)
        {
            _logger.TokenRevocationComplete();
            await _events.RaiseAsync(new TokenRevokedSuccessEvent(requestValidationResult, requestValidationResult.Client));
        }
        else
        {
            _logger.NoMatchingTokenFound();
        }

        if (response.Error.IsPresent()) return new TokenRevocationErrorResult(response.Error);

        return new StatusCodeResult(HttpStatusCode.OK);
    }
}
