namespace IdentityServer.Endpoints;

internal class DiscoveryKeyEndpoint : IEndpointHandler
{
    private readonly ILogger _logger;

    private readonly IdentityServerOptions _options;

    private readonly IDiscoveryResponseGenerator _responseGenerator;

    public DiscoveryKeyEndpoint(
        IdentityServerOptions options,
        IDiscoveryResponseGenerator responseGenerator,
        ILogger<DiscoveryKeyEndpoint> logger)
    {
        _logger = logger;
        _options = options;
        _responseGenerator = responseGenerator;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingKeyDiscoveryRequest();

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            _logger.DiscoveryEndpointOnlySupportsGetRequests();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        _logger.StartKeyDiscoveryRequest();

        if (_options.Discovery.ShowKeySet == false)
        {
            _logger.KeyDiscoveryDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        _logger.CallingIntoDiscoveryResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.CreateJwkDocumentAsync();

        return new JsonWebKeysResult(response, _options.Discovery.ResponseCacheInterval);
    }
}
