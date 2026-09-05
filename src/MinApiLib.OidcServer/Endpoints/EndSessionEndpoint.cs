using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Endpoints;

/// <summary>
/// The end session endpoint: RP-initiated logout. It accepts GET, with the parameters in the query, and POST, with
/// them in a form: <see cref="EndSessionGetEndpoint"/> and <see cref="EndSessionPostEndpoint"/>.
/// </summary>
internal abstract record EndSessionEndpoint(string Method, string Path) : EndpointHandlerAsync<EndSessionEndpoint.Request>(Method, Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IEndSessionRequestValidator EndSessionRequestValidator,
        [FromServices] IUserSession UserSession,
        [FromServices] ILogger<EndSessionEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.EndSession, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var logger = request.Logger;
        var parameters = await ReadParametersAsync(request.HttpContext.Request, cancellationToken);

        var user = await request.UserSession.GetUserAsync();
        logger.ProcessingSignoutRequest(user?.GetSubjectId() ?? "anonymous");

        var result = await request.EndSessionRequestValidator.ValidateAsync(parameters, user);

        if (result.IsError)
        {
            logger.ErrorProcessingEndSessionRequest(result.Error);
        }
        else
        {
            logger.SuccessValidatingEndSessionRequest(result.ValidatedRequest?.Client?.ClientId);
        }

        return new EndSessionResult(result);
    }

    /// <summary>
    /// Reads the end session request parameters.
    /// </summary>
    protected abstract Task<NameValueCollection> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// <c>GET</c> on the end session endpoint: the parameters are in the query.
/// </summary>
internal abstract record EndSessionGetEndpoint(string Path) : EndSessionEndpoint(HttpMethods.Get, Path)
{
    protected override Task<NameValueCollection> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Query.AsNameValueCollection());
}

/// <summary>
/// <c>POST</c> on the end session endpoint: the parameters are in a form.
/// </summary>
internal abstract record EndSessionPostEndpoint(string Path) : EndSessionEndpoint(HttpMethods.Post, Path)
{
    protected override async Task<NameValueCollection> ReadParametersAsync(HttpRequest request, CancellationToken cancellationToken)
        => (await request.ReadFormAsync(cancellationToken)).AsNameValueCollection();
}
