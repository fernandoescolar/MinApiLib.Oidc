using MinApiLib.Endpoints;

namespace MinApiLib.OidcServer.Features.Authorize;

/// <summary>
/// The authorize callback endpoint: resumes an authorize request after login or consent.
/// </summary>
internal abstract record AuthorizeCallbackEndpoint(string Path) : GetHandlerAsync<AuthorizeCallbackEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] AuthorizeRequestProcessor Processor,
        [FromServices] IUserSession UserSession,
        [FromServices] IConsentMessageStore ConsentResponseStore,
        [FromServices] IAuthorizationParametersMessageStore? AuthorizationParametersMessageStore,
        [FromServices] ILogger<AuthorizeCallbackEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Authorize, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.HttpContext;
        var logger = request.Logger;

        logger.StartAuthorizeCallbackRequest();

        var parameters = context.Request.Query.AsNameValueCollection();
        if (request.AuthorizationParametersMessageStore is { } parametersStore)
        {
            var messageStoreId = parameters[AuthorizationParamsStore.MessageStoreIdParameterName];
            var entry = await parametersStore.ReadAsync(messageStoreId);
            parameters = entry?.Data.FromFullDictionary() ?? [];

            await parametersStore.DeleteAsync(messageStoreId);
        }

        var user = await request.UserSession.GetUserAsync();
        var consentRequest = new ConsentRequest(parameters, user?.GetSubjectId());
        var consent = await request.ConsentResponseStore.ReadAsync(consentRequest.Id);

        if (consent != null && consent.Data == null)
        {
            return await request.Processor.CreateErrorResultAsync("consent message is missing data");
        }

        try
        {
            var result = await request.Processor.ProcessAuthorizeRequestAsync(parameters, user, consent?.Data);

            logger.EndAuthorizeRequest(result.GetType().ToString());

            return result;
        }
        finally
        {
            if (consent != null)
            {
                await request.ConsentResponseStore.DeleteAsync(consentRequest.Id);
            }
        }
    }
}

// The routes. MinApiLib maps every non-abstract endpoint record of the assembly: the endpoint's path and, for
// the connect/ endpoints, its connect/mtls/ alias, which answers 404 unless mutual TLS uses the path mode.
internal sealed record AuthorizeCallbackRoute() : AuthorizeCallbackEndpoint(OidcServerEndpoint.Route(ProtocolRoutePaths.AuthorizeCallback));
internal sealed record AuthorizeCallbackMutualTlsRoute() : AuthorizeCallbackEndpoint(OidcServerEndpoint.MutualTlsRoute(ProtocolRoutePaths.AuthorizeCallback));
