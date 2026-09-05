namespace IdentityServer.Extensions;

public static class HttpContextExtensions
{
    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return (handler is IAuthenticationSignOutHandler);
    }

    public static void SetIdentityServerOrigin(this HttpContext context, string value)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(value);

        var split = value.Split("://", StringSplitOptions.RemoveEmptyEntries);

        var request = context.Request;
        request.Scheme = split.First();
        request.Host = new HostString(split.Last());
    }

    public static void SetIdentityServerBasePath(this HttpContext context, string? value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[Constants.EnvironmentKeys.IdentityServerBasePath] = value;
    }

    public static string GetIdentityServerOrigin(this HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        var request = context.Request;

        if (options.MutualTls.Enabled && options.MutualTls.DomainName.IsPresent())
        {
            if (!options.MutualTls.DomainName.Contains('.'))
            {
                // the host of an incoming request is always set
                if (request.Host.Value!.StartsWith(options.MutualTls.DomainName + ".", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Concat(request.Scheme, "://", request.Host.Value.AsSpan(options.MutualTls.DomainName.Length + 1));
                }
            }
        }

        return request.Scheme + "://" + request.Host.Value;
    }

    internal static void SetSignOutCalled(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[Constants.EnvironmentKeys.SignOutCalled] = "true";
    }

    internal static bool GetSignOutCalled(this HttpContext context)
    {
        return context.Items.ContainsKey(Constants.EnvironmentKeys.SignOutCalled);
    }

    /// <summary>
    /// Gets the host name of IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string GetIdentityServerHost(this HttpContext context)
    {
        var request = context.Request;
        return request.Scheme + "://" + request.Host.ToUriComponent();
    }

    /// <summary>
    /// Gets the base path of IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string? GetIdentityServerBasePath(this HttpContext context)
    {
        return context.Items[Constants.EnvironmentKeys.IdentityServerBasePath] as string;
    }

    /// <summary>
    /// Gets the public base URL for IdentityServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string GetIdentityServerBaseUrl(this HttpContext context)
    {
        return context.GetIdentityServerHost() + context.GetIdentityServerBasePath();
    }

    /// <summary>
    /// Gets the identity server relative URL.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static string? GetIdentityServerRelativeUrl(this HttpContext context, string? path)
    {
        if (!path.IsLocalUrl())
        {
            return null;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal)) path = path.Substring(1);
        path = context.GetIdentityServerBaseUrl().EnsureTrailingSlash() + path.RemoveLeadingSlash();
        return path;
    }

    /// <summary>
    /// Gets the identity server issuer URI.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">context</exception>
    public static string GetIdentityServerIssuerUri(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        var uri = options.IssuerUri;
        if (uri.IsMissing())
        {
            uri = context.GetIdentityServerOrigin() + context.GetIdentityServerBasePath();
            if (uri.EndsWith('/')) uri = uri.Substring(0, uri.Length - 1);
            if (options.LowerCaseIssuerUri)
            {
                uri = uri.ToLowerInvariant();
            }
        }

        return uri;
    }

    internal static async Task<string?> GetIdentityServerSignoutFrameCallbackUrlAsync(this HttpContext context, LogoutMessage? logoutMessage = null)
    {
        var userSession = context.RequestServices.GetRequiredService<IUserSession>();
        var user = await userSession.GetUserAsync();
        var currentSubId = user?.GetSubjectId();

        LogoutNotificationContext? endSessionMsg = null;

        if (logoutMessage?.ClientIds?.Any() == true)
        {
            var clientIds = logoutMessage.ClientIds;

            if (currentSubId == logoutMessage.SubjectId)
            {
                clientIds = clientIds.Union(await userSession.GetClientListAsync());
                clientIds = clientIds.Distinct();
            }

            endSessionMsg = new LogoutNotificationContext
            {
                SubjectId = logoutMessage.SubjectId,
                SessionId = logoutMessage.SessionId,
                ClientIds = clientIds
            };
        }
        else if (currentSubId != null)
        {
            var clientIds = await userSession.GetClientListAsync();
            if (clientIds.Any())
            {
                endSessionMsg = new LogoutNotificationContext
                {
                    SubjectId = currentSubId,
                    SessionId = await userSession.GetSessionIdAsync(),
                    ClientIds = clientIds
                };
            }
        }

        if (endSessionMsg != null)
        {
            var clock = context.RequestServices.GetRequiredService<TimeProvider>();
            var msg = new Message<LogoutNotificationContext>(endSessionMsg, clock.GetUtcNow().UtcDateTime);

            var endSessionMessageStore = context.RequestServices.GetRequiredService<IMessageStore<LogoutNotificationContext>>();
            var id = await endSessionMessageStore.WriteAsync(msg);

            var signoutIframeUrl = context.GetIdentityServerBaseUrl().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.EndSessionCallback;
            // a failed write (null id) throws in AddQueryString, as it always has
            signoutIframeUrl = signoutIframeUrl.AddQueryString(Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback, id!);

            return signoutIframeUrl;
        }

        return null;
    }
}
