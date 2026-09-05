namespace IdentityServer.Endpoints;

internal class DiscoveryEndpoint : IEndpointHandler
{
    private readonly ILogger _logger;

    private readonly IdentityServerOptions _options;

    private readonly IDiscoveryResponseGenerator _responseGenerator;

    public DiscoveryEndpoint(
        IdentityServerOptions options,
        IDiscoveryResponseGenerator responseGenerator,
        ILogger<DiscoveryEndpoint> logger)
    {
        _logger = logger;
        _options = options;
        _responseGenerator = responseGenerator;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingDiscoveryRequest();

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            _logger.DiscoveryEndpointOnlySupportsGetRequests();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        _logger.StartDiscoveryRequest();

        if (!_options.Endpoints.EnableDiscoveryEndpoint)
        {
            _logger.DiscoveryEndpointDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        var baseUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash();
        var issuerUri = context.GetIdentityServerIssuerUri();

        _logger.CallingIntoDiscoveryResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

        return new DiscoveryDocumentResult(response, _options.Discovery.ResponseCacheInterval);
    }
}
