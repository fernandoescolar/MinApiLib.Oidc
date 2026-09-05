namespace MinApiLib.OidcServer.Hosting;

/// <summary>
/// OidcServer middleware. Keeps the session cookie in sync and triggers logout notifications after a sign-out.
/// The protocol endpoints themselves are served by endpoint routing, see <c>MapOidcServer()</c>.
/// </summary>
public class OidcServerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcServerMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="logger">The logger.</param>
    public OidcServerMiddleware(RequestDelegate next, ILogger<OidcServerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="session">The user session.</param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context, IUserSession session)
    {
        context.RequestServices.GetRequiredService<OidcServerEndpointsMarker>().EnsureMapped();

        await session.EnsureSessionIdCookieAsync();

        context.Response.OnStarting(async () =>
        {
            if (context.GetSignOutCalled())
            {
                ResponseAndHostingLog.ProcessingPostSignOutCleanup(_logger);

                await session.RemoveSessionIdCookieAsync();

                var logoutContext = await session.GetLogoutNotificationContext();
                if (logoutContext != null)
                {
                    await SendBackChannelLogoutNotificationsAsync(context, logoutContext);
                }
            }
        });

        await _next(context);
    }

    private async Task SendBackChannelLogoutNotificationsAsync(HttpContext context, LogoutNotificationContext logoutContext)
    {
        var options = context.RequestServices.GetRequiredService<OidcServerOptions>();
        if (options.Authentication.SendBackChannelLogoutNotificationsInBackground)
        {
            var queue = context.RequestServices.GetService<BackChannelLogoutQueue>();
            if (queue != null && queue.TryEnqueue(BackChannelLogoutWorkItem.Create(context, logoutContext)))
            {
                ResponseAndHostingLog.BackChannelLogoutQueued(_logger, logoutContext.SessionId);
                return;
            }
        }

        await context.RequestServices.GetRequiredService<IBackChannelLogoutService>().SendLogoutNotificationsAsync(logoutContext);
    }
}
