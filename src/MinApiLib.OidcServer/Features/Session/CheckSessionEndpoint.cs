using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Features.Session;

/// <summary>
/// The check session endpoint: the iframe of OpenID Connect Session Management.
/// </summary>
internal abstract record CheckSessionEndpoint(string Path) : GetHandlerAsync<CheckSessionEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] ILogger<CheckSessionEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.CheckSession, Path, GetType());

    protected override Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        request.Logger.RenderingCheckSessionResult();
        return Task.FromResult<IResult>(new CheckSessionResult());
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record CheckSessionRoute() : CheckSessionEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.CheckSession));
internal sealed record CheckSessionMutualTlsRoute() : CheckSessionEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.CheckSession));
