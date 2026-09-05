using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

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
