using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Features.Session;

/// <summary>
/// The end session callback endpoint: renders the front-channel logout iframes after a sign-out.
/// </summary>
internal abstract record EndSessionCallbackEndpoint(string Path) : GetHandlerAsync<EndSessionCallbackEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IEndSessionRequestValidator EndSessionRequestValidator,
        [FromServices] ILogger<EndSessionCallbackEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.EndSession, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;

        logger.ProcessingSignoutCallbackRequest();

        var parameters = context.Request.Query.AsNameValueCollection();
        var result = await request.EndSessionRequestValidator.ValidateCallbackAsync(parameters);

        if (!result.IsError)
        {
            logger.SuccessfulSignoutCallback();
        }
        else
        {
            logger.ErrorValidatingSignoutCallback(result.Error);
        }

        return new EndSessionCallbackResult(result);
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record EndSessionCallbackRoute() : EndSessionCallbackEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.EndSessionCallback));
internal sealed record EndSessionCallbackMutualTlsRoute() : EndSessionCallbackEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.EndSessionCallback));
