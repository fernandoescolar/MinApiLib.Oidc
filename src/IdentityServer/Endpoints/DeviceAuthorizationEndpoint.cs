namespace IdentityServer.Endpoints;

/// <summary>
/// The device authorization endpoint
/// </summary>
/// <seealso cref="IdentityServer.Hosting.IEndpointHandler" />
internal class DeviceAuthorizationEndpoint : IEndpointHandler
{
    private readonly IClientSecretValidator _clientValidator;
    private readonly IDeviceAuthorizationRequestValidator _requestValidator;
    private readonly IDeviceAuthorizationResponseGenerator _responseGenerator;
    private readonly IEventService _events;
    private readonly ILogger<DeviceAuthorizationEndpoint> _logger;

    public DeviceAuthorizationEndpoint(
        IClientSecretValidator clientValidator,
        IDeviceAuthorizationRequestValidator requestValidator,
        IDeviceAuthorizationResponseGenerator responseGenerator,
        IEventService events,
        ILogger<DeviceAuthorizationEndpoint> logger)
    {
        _clientValidator = clientValidator;
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
    /// <exception cref="System.NotImplementedException"></exception>
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingDeviceAuthorizeRequest();

        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
        {
            _logger.InvalidHttpRequestForDeviceAuthorizeEndpoint();
            return Error(OidcConstants.TokenErrors.InvalidRequest);
        }

        return await ProcessDeviceAuthorizationRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessDeviceAuthorizationRequestAsync(HttpContext context)
    {
        _logger.StartDeviceAuthorizeRequest();

        var clientResult = await _clientValidator.ValidateAsync(context);
        if (clientResult.Client == null) return Error(OidcConstants.TokenErrors.InvalidClient);

        var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
        var requestResult = await _requestValidator.ValidateAsync(form, clientResult);

        if (requestResult.IsError)
        {
            await _events.RaiseAsync(new DeviceAuthorizationFailureEvent(requestResult));
            return Error(requestResult.Error, requestResult.ErrorDescription);
        }

        var baseUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash();

        _logger.CallingIntoDeviceAuthorizeResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(requestResult, baseUrl);

        await _events.RaiseAsync(new DeviceAuthorizationSuccessEvent(requestResult));

        _logger.DeviceAuthorizeRequestSuccess();
        return new DeviceAuthorizationResult(response);
    }

    private TokenErrorResult Error(string error, string? errorDescription = null, Dictionary<string, object>? custom = null)
    {
        var response = new TokenErrorResponse
        {
            Error = error,
            ErrorDescription = errorDescription,
            Custom = custom
        };

        _logger.DeviceAuthorizationError(error, error ?? "-no message-");

        return new TokenErrorResult(response);
    }
}
