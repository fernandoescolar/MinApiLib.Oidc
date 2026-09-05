using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The discovery document endpoint.
/// </summary>
internal abstract record DiscoveryEndpoint(string Path) : GetHandlerAsync<DiscoveryEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] OidcServerOptions Options,
        [FromServices] IDiscoveryResponseGenerator ResponseGenerator,
        [FromServices] ILogger<DiscoveryEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Discovery, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;
        logger.ProcessingDiscoveryRequest();

        logger.StartDiscoveryRequest();

        if (!request.Options.Endpoints.EnableDiscoveryEndpoint)
        {
            logger.DiscoveryEndpointDisabled();
            return new StatusCodeResult(HttpStatusCode.NotFound);
        }

        var baseUrl = context.GetOidcServerBaseUrl().EnsureTrailingSlash();
        var issuerUri = context.GetOidcServerIssuerUri();

        logger.CallingIntoDiscoveryResponseGenerator(request.ResponseGenerator.GetType().FullName);
        var response = await request.ResponseGenerator.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

        return new DiscoveryDocumentResult(response, request.Options.Discovery.ResponseCacheInterval);
    }
}
