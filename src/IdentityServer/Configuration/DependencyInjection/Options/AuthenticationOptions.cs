namespace IdentityServer.Configuration;

/// <summary>
/// Configures the login and logout views and behavior.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// Sets the cookie authentication scheme configured by the host used for interactive users. If not set, the scheme will inferred from the host's default authentication scheme.
    /// This setting is typically used when AddPolicyScheme is used in the host as the default scheme.
    /// </summary>
    public string? CookieAuthenticationScheme { get; set; }

    /// <summary>
    /// Sets the cookie lifetime (only effective if the IdentityServer-provided cookie handler is used)
    /// </summary>
    public TimeSpan CookieLifetime { get; set; } = Constants.DefaultCookieTimeSpan;

    /// <summary>
    /// Specified if the cookie should be sliding or not (only effective if the built-in cookie middleware is used)
    /// </summary>
    public bool CookieSlidingExpiration { get; set; }

    /// <summary>
    /// Specifies the SameSite mode for the internal authentication and temp cookie
    /// </summary>
    public Microsoft.AspNetCore.Http.SameSiteMode CookieSameSiteMode { get; set; } = Microsoft.AspNetCore.Http.SameSiteMode.None;

    /// <summary>
    /// Indicates if user must be authenticated to accept parameters to end session endpoint. Defaults to false.
    /// </summary>
    /// <value>
    /// <c>true</c> if required; otherwise, <c>false</c>.
    /// </value>
    public bool RequireAuthenticatedUserForSignOutMessage { get; set; }

    /// <summary>
    /// Gets or sets the name of the cookie used for the check session endpoint.
    /// </summary>
    public string CheckSessionCookieName { get; set; } = IdentityServerConstants.DefaultCheckSessionCookieName;

    /// <summary>
    /// Gets or sets the domain of the cookie used for the check session endpoint. Defaults to null.
    /// </summary>
    public string? CheckSessionCookieDomain { get; set; }

    /// <summary>
    /// Gets or sets the SameSite mode of the cookie used for the check session endpoint. Defaults to SameSiteMode.None.
    /// </summary>
    public Microsoft.AspNetCore.Http.SameSiteMode CheckSessionCookieSameSiteMode { get; set; } = Microsoft.AspNetCore.Http.SameSiteMode.None;

    /// <summary>
    /// If set, will require frame-src CSP headers being emitting on the end session callback endpoint which renders iframes to clients for front-channel signout notification.
    /// </summary>
    public bool RequireCspFrameSrcForSignout { get; set; } = true;

    /// <summary>
    /// Gets or sets whether back-channel logout notifications are sent by a background service instead of before
    /// the sign-out response starts. Slow or unreachable clients then do not delay the user's sign-out.
    /// Defaults to <c>true</c>. Set it to <c>false</c> when a custom <c>IBackChannelLogoutService</c> needs the
    /// original HTTP request.
    /// </summary>
    public bool SendBackChannelLogoutNotificationsInBackground { get; set; } = true;
}
