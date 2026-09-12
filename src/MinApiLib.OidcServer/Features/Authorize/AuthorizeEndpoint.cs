using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Features.Authorize;

/// <summary>
/// The authorize endpoint. OpenID Connect requires both GET, with the parameters in the query, and POST, with them in
/// a form: <see cref="AuthorizeGetEndpoint"/> and <see cref="AuthorizePostEndpoint"/>.
/// </summary>
internal abstract record AuthorizeEndpoint(string Method, string Path) : EndpointHandlerAsync<AuthorizeEndpoint.Request>(Method, Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] AuthorizeRequestProcessor Processor,
        [FromServices] IUserSession UserSession,
        [FromServices] ILogger<AuthorizeEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Authorize, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        request.Logger.StartAuthorizeRequest();

        var values = await ReadParametersAsync(request.HttpContext.Request, cancellationToken);
        if (values == null)
        {
            return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
        }

        var user = await request.UserSession.GetUserAsync();
        var result = await request.Processor.ProcessAuthorizeRequestAsync(values, user, null);

        request.Logger.EndAuthorizeRequest(result.GetType().ToString());

        return result;
    }

    /// <summary>
    /// Reads the authorize request parameters, or returns <see langword="null"/> when the request has none to read.
    /// </summary>
    protected abstract Task<NameValueCollection?> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// <c>GET</c> on the authorize endpoint: the parameters are in the query.
/// </summary>
internal abstract record AuthorizeGetEndpoint(string Path) : AuthorizeEndpoint(HttpMethods.Get, Path)
{
    protected override Task<NameValueCollection?> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken)
        => Task.FromResult<NameValueCollection?>(request.Query.AsNameValueCollection());
}

/// <summary>
/// <c>POST</c> on the authorize endpoint: the parameters are in an <c>application/x-www-form-urlencoded</c> form.
/// </summary>
internal abstract record AuthorizePostEndpoint(string Path) : AuthorizeEndpoint(HttpMethods.Post, Path)
{
    protected override async Task<NameValueCollection?> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasApplicationFormContentType())
        {
            return null;
        }

        return (await request.ReadFormAsync(cancellationToken)).AsNameValueCollection();
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record AuthorizeGetRoute() : AuthorizeGetEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizeGetMutualTlsRoute() : AuthorizeGetEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizePostRoute() : AuthorizePostEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.Authorize));
internal sealed record AuthorizePostMutualTlsRoute() : AuthorizePostEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.Authorize));
