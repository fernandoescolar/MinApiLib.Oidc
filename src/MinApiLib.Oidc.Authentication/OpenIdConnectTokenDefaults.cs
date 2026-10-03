namespace MinApiLib.Oidc.Authentication;

/// <summary>
/// Names used by the library.
/// </summary>
public static class OpenIdConnectTokenDefaults
{
    /// <summary>
    /// The authentication property that keeps the user's DPoP key, as a private JSON Web Key, in the session.
    /// </summary>
    public const string DPoPKeyProperty = "MinApiLib.Oidc.Authentication.DPoPKey";

    /// <summary>
    /// The authentication property that records which OpenID Connect scheme signed the user in, to refresh the
    /// tokens with it.
    /// </summary>
    public const string SchemeProperty = "MinApiLib.Oidc.Authentication.Scheme";
}
