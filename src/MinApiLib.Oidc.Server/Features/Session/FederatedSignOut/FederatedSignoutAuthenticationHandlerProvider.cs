namespace MinApiLib.Oidc.Server.Features.Session.FederatedSignOut;

internal class FederatedSignoutAuthenticationHandlerProvider : IAuthenticationHandlerProvider
{
    private readonly IAuthenticationHandlerProvider _provider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FederatedSignoutAuthenticationHandlerProvider(
        [FromKeyedServices(DecoratorServiceCollectionExtensions.InnerServiceKey)] IAuthenticationHandlerProvider inner,
        IHttpContextAccessor httpContextAccessor)
    {
        _provider = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IAuthenticationHandler?> GetHandlerAsync(HttpContext context, string authenticationScheme)
    {
        var handler = await _provider.GetHandlerAsync(context, authenticationScheme);
        if (handler is IAuthenticationRequestHandler requestHandler)
        {
            if (requestHandler is IAuthenticationSignInHandler signinHandler)
            {
                return new AuthenticationRequestSignInHandlerWrapper(signinHandler, _httpContextAccessor);
            }

            if (requestHandler is IAuthenticationSignOutHandler signoutHandler)
            {
                return new AuthenticationRequestSignOutHandlerWrapper(signoutHandler, _httpContextAccessor);
            }

            return new AuthenticationRequestHandlerWrapper(requestHandler, _httpContextAccessor);
        }

        return handler;
    }
}
