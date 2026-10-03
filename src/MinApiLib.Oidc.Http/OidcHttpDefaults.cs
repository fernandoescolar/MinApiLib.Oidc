namespace MinApiLib.Oidc.Http;

/// <summary>
/// Names used by the library.
/// </summary>
public static class OidcHttpDefaults
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> that requests client credentials tokens and reads discovery
    /// documents. Configure it with <c>services.AddHttpClient(OidcHttpDefaults.HttpClientName)</c>, for example to
    /// add a proxy or a client certificate.
    /// </summary>
    public const string HttpClientName = "MinApiLib.Oidc.Http";
}
