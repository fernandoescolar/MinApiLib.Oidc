namespace IdentityServer.Endpoints;

/// <summary>
/// Introspection endpoint
/// </summary>
/// <seealso cref="IdentityServer.Hosting.IEndpointHandler" />
internal class IntrospectionEndpoint : IEndpointHandler
{
    private readonly IIntrospectionResponseGenerator _responseGenerator;
    private readonly IEventService _events;
    private readonly ILogger _logger;
    private readonly IIntrospectionRequestValidator _requestValidator;
    private readonly IApiSecretValidator _apiSecretValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectionEndpoint" /> class.
    /// </summary>
    /// <param name="apiSecretValidator">The API secret validator.</param>
    /// <param name="requestValidator">The request validator.</param>
    /// <param name="responseGenerator">The generator.</param>
    /// <param name="events">The events.</param>
    /// <param name="logger">The logger.</param>
    public IntrospectionEndpoint(
        IApiSecretValidator apiSecretValidator,
        IIntrospectionRequestValidator requestValidator,
        IIntrospectionResponseGenerator responseGenerator,
        IEventService events,
        ILogger<IntrospectionEndpoint> logger)
    {
        _apiSecretValidator = apiSecretValidator;
        _requestValidator = requestValidator;
        _responseGenerator = responseGenerator;
        _events = events;
        _logger = logger;
    }

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingIntrospectionRequest();

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            _logger.IntrospectionEndpointOnlySupportsPostRequests();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        if (!context.Request.HasApplicationFormContentType())
        {
            _logger.InvalidMediaTypeForIntrospectionEndpoint();
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        return await ProcessIntrospectionRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessIntrospectionRequestAsync(HttpContext context)
    {
        _logger.StartingIntrospectionRequest();

        var apiResult = await _apiSecretValidator.ValidateAsync(context);
        if (apiResult.Resource == null)
        {
            _logger.APIUnauthorizedToCallIntrospectionEndpoint();
            return new StatusCodeResult(HttpStatusCode.Unauthorized);
        }

        var body = await context.Request.ReadFormAsync();

        _logger.CallingIntoIntrospectionRequestValidator(_requestValidator.GetType().FullName);
        var validationResult = await _requestValidator.ValidateAsync(body.AsNameValueCollection(), apiResult.Resource);
        if (validationResult.IsError)
        {
            _logger.FailedTokenIntrospection(validationResult.Error, apiResult.Resource.Name);
            await _events.RaiseAsync(new TokenIntrospectionFailureEvent(apiResult.Resource.Name, validationResult.Error));

            return new BadRequestResult(validationResult.Error);
        }

        _logger.CallingIntoIntrospectionResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(validationResult);

        _logger.SuccessTokenIntrospection(validationResult.IsActive, validationResult.Api.Name);
        return new IntrospectionResult(response);
    }
}
