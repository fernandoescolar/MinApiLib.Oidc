// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Extensions;

public static class HttpContextExtensions
{
    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return (handler is IAuthenticationSignOutHandler);
    }

    public static void SetOidcServerOrigin(this HttpContext context, string value)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(value);

        var split = value.Split("://", StringSplitOptions.RemoveEmptyEntries);

        var request = context.Request;
        request.Scheme = split.First();
        request.Host = new HostString(split.Last());
    }

    public static void SetOidcServerBasePath(this HttpContext context, string? value)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[Constants.EnvironmentKeys.OidcServerBasePath] = value;
    }

    public static string GetOidcServerOrigin(this HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<OidcServerOptions>();
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
    /// Gets the host name of OidcServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string GetOidcServerHost(this HttpContext context)
    {
        var request = context.Request;
        return request.Scheme + "://" + request.Host.ToUriComponent();
    }

    /// <summary>
    /// Gets the base path of OidcServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string? GetOidcServerBasePath(this HttpContext context)
    {
        return context.Items[Constants.EnvironmentKeys.OidcServerBasePath] as string;
    }

    /// <summary>
    /// Gets the public base URL for OidcServer.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public static string GetOidcServerBaseUrl(this HttpContext context)
    {
        return context.GetOidcServerHost() + context.GetOidcServerBasePath();
    }

    /// <summary>
    /// Gets the identity server relative URL.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static string? GetOidcServerRelativeUrl(this HttpContext context, string? path)
    {
        if (!path.IsLocalUrl())
        {
            return null;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal)) path = path.Substring(1);
        path = context.GetOidcServerBaseUrl().EnsureTrailingSlash() + path.RemoveLeadingSlash();
        return path;
    }

    /// <summary>
    /// Gets the identity server issuer URI.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">context</exception>
    public static string GetOidcServerIssuerUri(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = context.RequestServices.GetRequiredService<OidcServerOptions>();
        var uri = options.IssuerUri;
        if (uri.IsMissing())
        {
            uri = context.GetOidcServerOrigin() + context.GetOidcServerBasePath();
            if (uri.EndsWith('/')) uri = uri.Substring(0, uri.Length - 1);
            if (options.LowerCaseIssuerUri)
            {
                uri = uri.ToLowerInvariant();
            }
        }

        return uri;
    }

    internal static async Task<string?> GetOidcServerSignoutFrameCallbackUrlAsync(this HttpContext context, LogoutMessage? logoutMessage = null)
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

            var signoutIframeUrl = context.GetOidcServerBaseUrl().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.EndSessionCallback;
            // a failed write (null id) throws in AddQueryString, as it always has
            signoutIframeUrl = signoutIframeUrl.AddQueryString(Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback, id!);

            return signoutIframeUrl;
        }

        return null;
    }
}
