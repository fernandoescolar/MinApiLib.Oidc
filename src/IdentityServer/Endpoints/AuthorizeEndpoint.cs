namespace IdentityServer.Endpoints;

internal class AuthorizeEndpoint : AuthorizeEndpointBase
{
    public AuthorizeEndpoint(
        IEventService events,
        ILogger<AuthorizeEndpoint> logger,
        IdentityServerOptions options,
        IAuthorizeRequestValidator validator,
        IAuthorizeInteractionResponseGenerator interactionGenerator,
        IAuthorizeResponseGenerator authorizeResponseGenerator,
        IUserSession userSession,
        IPushedAuthorizationRequestStore? pushedAuthorizationRequestStore = null)
        : base(events, logger, options, validator, interactionGenerator, authorizeResponseGenerator, userSession, pushedAuthorizationRequestStore)
    {
    }

    public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        Logger.StartAuthorizeRequest();

        NameValueCollection values;

        if (HttpMethods.IsGet(context.Request.Method))
        {
            values = context.Request.Query.AsNameValueCollection();
        }
        else if (HttpMethods.IsPost(context.Request.Method))
        {
            if (!context.Request.HasApplicationFormContentType())
            {
                return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
            }

            values = context.Request.Form.AsNameValueCollection();
        }
        else
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        var user = await UserSession.GetUserAsync();
        var result = await ProcessAuthorizeRequestAsync(values, user, null);

        Logger.EndAuthorizeRequest(result?.GetType().ToString() ?? "-none-");

        return result!;
    }
}
