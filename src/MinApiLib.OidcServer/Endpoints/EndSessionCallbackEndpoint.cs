using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

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
