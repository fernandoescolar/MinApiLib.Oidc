namespace IdentityServer;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0x0F01, LogLevel.Debug, "Login Url: {LoginUrl}\nLogin Return Url Parameter: {LoginReturnUrlParameter}\nLogout Url: {LogoutUrl}\nConsent Url: {ConsentUrl}\nConsent Return Url Parameter: {ConsentReturnUrlParameter}\nError Url: {ErrorUrl}\nError Id Parameter: {ErrorIdParameter}")]
    public static partial void PostConfigure(this ILogger logger, string LoginUrl, string LoginReturnUrlParameter, string LogoutUrl, string ConsentUrl, string ConsentReturnUrlParameter, string ErrorUrl, string ErrorIdParameter);

    [LoggerMessage(0x0F11, LogLevel.Information, "Starting IdentityServer version {version}")]
    public static partial void StartingIdentityServer(this ILogger logger, string version);

    [LoggerMessage(0x0F12, LogLevel.Information, "Custom IssuerUri set to {issuerUri}")]
    public static partial void CustomIssuerUri(this ILogger logger, string issuerUri);

    [LoggerMessage(0x0F13, LogLevel.Critical, "Required service {service} is not registered in the DI container. Aborting startup")]
    public static partial void RequiredServiceNotRegistered(this ILogger logger, string service);

    [LoggerMessage(0x0F15, LogLevel.Information, "You are using the in-memory version of the persisted grant store. This will store consent decisions, authorization codes, refresh and reference tokens in memory only. If you are using any of those features in production, you want to switch to a different store implementation.")]
    public static partial void UsingInMemoryPersistedGrantStore(this ILogger logger);

    [LoggerMessage(0x0F23, LogLevel.Warning, "IssuerUri is not configured, so the issuer is derived from the Host header of each request. Set IssuerUri, or restrict accepted hosts (AllowedHosts or a reverse proxy), to prevent issuer spoofing.")]
    public static partial void IssuerUriNotConfigured(this ILogger logger);

    [LoggerMessage(0x0F24, LogLevel.Warning, "The in-memory persisted grant store is used outside Development. Codes, refresh tokens, reference tokens and consents are lost on restart and are not shared between instances.")]
    public static partial void InMemoryPersistedGrantStoreOutsideDevelopment(this ILogger logger);

    [LoggerMessage(0x0F25, LogLevel.Warning, "AddDeveloperSigningCredential is used outside Development. Its private key is generated locally and may be stored unencrypted on disk; configure a managed signing key instead.")]
    public static partial void DeveloperSigningCredentialOutsideDevelopment(this ILogger logger);

    [LoggerMessage(0x0F16, LogLevel.Warning, "No authentication scheme has been set. Setting either a default authentication scheme or a CookieAuthenticationScheme on IdentityServerOptions is required.")]
    public static partial void NoAuthenticationScheme(this ILogger logger);

    [LoggerMessage(0x0F17, LogLevel.Information, "Using explicitly configured authentication scheme {scheme} for IdentityServer")]
    public static partial void UsingExplicitAuthenticationScheme(this ILogger logger, string scheme);

    [LoggerMessage(0x0F18, LogLevel.Information, "Using the default authentication scheme {scheme} for IdentityServer")]
    public static partial void UsingDefaultAuthenticationScheme(this ILogger logger, string scheme);

    [LoggerMessage(0x0F19, LogLevel.Information, "Authentication scheme {scheme} is configured for IdentityServer, but it is not a scheme that supports signin (like cookies). If you support interactive logins via the browser, then a cookie-based scheme should be used.")]
    public static partial void AuthenticationSchemeNotSupportingSignin(this ILogger logger, string scheme);

    [LoggerMessage(0x0F1A, LogLevel.Debug, "Using {scheme} as default ASP.NET Core scheme for authentication")]
    public static partial void DefaultAuthenticationScheme(this ILogger logger, string? scheme);

    [LoggerMessage(0x0F1B, LogLevel.Debug, "Using {scheme} as default ASP.NET Core scheme for sign-in")]
    public static partial void DefaultSignInScheme(this ILogger logger, string? scheme);

    [LoggerMessage(0x0F1C, LogLevel.Debug, "Using {scheme} as default ASP.NET Core scheme for sign-out")]
    public static partial void DefaultSignOutScheme(this ILogger logger, string? scheme);

    [LoggerMessage(0x0F1D, LogLevel.Debug, "Using {scheme} as default ASP.NET Core scheme for challenge")]
    public static partial void DefaultChallengeScheme(this ILogger logger, string? scheme);

    [LoggerMessage(0x0F1E, LogLevel.Debug, "Using {scheme} as default ASP.NET Core scheme for forbid")]
    public static partial void DefaultForbidScheme(this ILogger logger, string? scheme);
}
