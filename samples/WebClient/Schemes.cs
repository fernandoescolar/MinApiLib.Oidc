namespace Samples.WebClient;

public static class Schemes
{
    public const string Cookie = "cookie";

    /// <summary>Signs in with the "web" client: bearer access tokens.</summary>
    public const string Bearer = "oidc";

    /// <summary>Signs in with the "web.dpop" client: DPoP-bound access tokens.</summary>
    public const string DPoP = "oidc-dpop";

    /// <summary>The OpenID Connect scheme that signed the user in, which AddOpenIdConnectTokens records in the session.</summary>
    public static string? Of(AuthenticationProperties? properties)
        => properties?.Items.TryGetValue(OpenIdConnectTokenDefaults.SchemeProperty, out var scheme) == true ? scheme : null;
}
