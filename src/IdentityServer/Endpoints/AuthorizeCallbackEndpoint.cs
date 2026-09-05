namespace IdentityServer.Endpoints;

internal class AuthorizeCallbackEndpoint : AuthorizeEndpointBase
{
    private readonly IConsentMessageStore _consentResponseStore;
    private readonly IAuthorizationParametersMessageStore? _authorizationParametersMessageStore;

    public AuthorizeCallbackEndpoint(
        IEventService events,
        ILogger<AuthorizeCallbackEndpoint> logger,
        IdentityServerOptions options,
        IAuthorizeRequestValidator validator,
        IAuthorizeInteractionResponseGenerator interactionGenerator,
        IAuthorizeResponseGenerator authorizeResponseGenerator,
        IUserSession userSession,
        IConsentMessageStore consentResponseStore,
        IAuthorizationParametersMessageStore? authorizationParametersMessageStore = null,
        IPushedAuthorizationRequestStore? pushedAuthorizationRequestStore = null)
        : base(events, logger, options, validator, interactionGenerator, authorizeResponseGenerator, userSession, pushedAuthorizationRequestStore)
    {
        _consentResponseStore = consentResponseStore;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            Logger.InvalidHttpMethodForAuthorizeEndpoint();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        Logger.StartAuthorizeCallbackRequest();

        var parameters = context.Request.Query.AsNameValueCollection();
        if (_authorizationParametersMessageStore != null)
        {
            var messageStoreId = parameters[AuthorizationParamsStore.MessageStoreIdParameterName];
            var entry = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
            parameters = entry?.Data.FromFullDictionary() ?? [];

            await _authorizationParametersMessageStore.DeleteAsync(messageStoreId);
        }

        var user = await UserSession.GetUserAsync();
        var consentRequest = new ConsentRequest(parameters, user?.GetSubjectId());
        var consent = await _consentResponseStore.ReadAsync(consentRequest.Id);

        if (consent != null && consent.Data == null)
        {
            return await CreateErrorResultAsync("consent message is missing data");
        }

        try
        {
            var result = await ProcessAuthorizeRequestAsync(parameters, user, consent?.Data);

            Logger.EndAuthorizeRequest(result?.GetType().ToString() ?? "-none-");

            return result!;
        }
        finally
        {
            if (consent != null)
            {
                await _consentResponseStore.DeleteAsync(consentRequest.Id);
            }
        }
    }
}
