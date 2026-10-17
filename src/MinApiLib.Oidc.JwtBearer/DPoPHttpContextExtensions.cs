namespace MinApiLib.Oidc.JwtBearer;

/// <summary>
/// How the current request was authenticated, for endpoints.
/// </summary>
public static class DPoPHttpContextExtensions
{
    internal const string SchemeItem = "MinApiLib.Oidc.JwtBearer.Scheme";
    internal const string AccessTokenItem = "MinApiLib.Oidc.JwtBearer.AccessToken";
    internal const string ThumbprintItem = "MinApiLib.Oidc.JwtBearer.Thumbprint";
    internal const string FailureItem = "MinApiLib.Oidc.JwtBearer.Failure";

    /// <summary>The <c>Authorization</c> scheme of the access token: <c>Bearer</c>, <c>DPoP</c> or <see langword="null"/>.</summary>
    public static string? GetAccessTokenScheme(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(SchemeItem, out var scheme) ? scheme as string : null;
    }

    /// <summary>
    /// The thumbprint of the key the access token is bound to, whose possession the request proved, or
    /// <see langword="null"/> for a bearer token.
    /// </summary>
    public static string? GetDPoPThumbprint(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(ThumbprintItem, out var thumbprint) ? thumbprint as string : null;
    }
}
