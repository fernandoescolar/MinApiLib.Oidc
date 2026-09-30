namespace MinApiLib.OidcServer.Client;

/// <summary>
/// Names used by the library.
/// </summary>
public static class OidcClientDefaults
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> that requests client credentials tokens and reads discovery
    /// documents. Configure it with <c>services.AddHttpClient(OidcClientDefaults.HttpClientName)</c>, for example to
    /// add a proxy or a client certificate. The OpenID Connect schemes use their own back-channel instead.
    /// </summary>
    public const string HttpClientName = "MinApiLib.OidcServer.Client";

    /// <summary>
    /// The authentication property that keeps the user's DPoP key, as a private JSON Web Key, in the session.
    /// </summary>
    public const string DPoPKeyProperty = "MinApiLib.OidcServer.Client.DPoPKey";

    /// <summary>
    /// The authentication property that records which OpenID Connect scheme signed the user in, to refresh the
    /// tokens with it.
    /// </summary>
    public const string SchemeProperty = "MinApiLib.OidcServer.Client.Scheme";
}
