using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The JSON Web Key Set endpoint of discovery.
/// </summary>
internal abstract record DiscoveryKeyEndpoint(string Path) : GetHandlerAsync<DiscoveryKeyEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] OidcServerOptions Options,
        [FromServices] IDiscoveryResponseGenerator ResponseGenerator,
        [FromServices] ILogger<DiscoveryKeyEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Discovery, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var logger = request.Logger;
        logger.ProcessingKeyDiscoveryRequest();

        logger.StartKeyDiscoveryRequest();

        if (request.Options.Discovery.ShowKeySet == false)
        {
            logger.KeyDiscoveryDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        logger.CallingIntoDiscoveryResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.CreateJwkDocumentAsync();

        return new JsonWebKeysResult(response, request.Options.Discovery.ResponseCacheInterval);
    }
}
