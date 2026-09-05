namespace IdentityServer.Hosting;

/// <summary>
/// A queued set of back-channel logout notifications, with the request data needed to resolve the issuer
/// without the original HTTP request.
/// </summary>
internal sealed record BackChannelLogoutWorkItem(
    LogoutNotificationContext Context,
    string Scheme,
    string? Host,
    string? PathBase,
    string? IdentityServerBasePath)
{
    public static BackChannelLogoutWorkItem Create(HttpContext httpContext, LogoutNotificationContext context) => new(
        context,
        httpContext.Request.Scheme,
        httpContext.Request.Host.Value,
        httpContext.Request.PathBase.Value,
        httpContext.GetIdentityServerBasePath());

    /// <summary>
    /// Builds a minimal HTTP context with the original scheme, host and base path, so the issuer and any
    /// URL derived from the request resolve as they did during the sign-out request.
    /// </summary>
    public HttpContext CreateHttpContext(IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Scheme = Scheme;
        httpContext.Request.Host = new HostString(Host);
        httpContext.Request.PathBase = new PathString(PathBase);
        httpContext.SetIdentityServerBasePath(IdentityServerBasePath);
        return httpContext;
    }
}
