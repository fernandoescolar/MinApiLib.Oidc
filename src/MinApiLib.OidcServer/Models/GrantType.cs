namespace MinApiLib.OidcServer.Models;

public static class GrantType
{
    /// <summary>
    /// The implicit grant (<c>response_type</c> <c>id_token</c>, <c>id_token token</c> or <c>token</c>). Deprecated
    /// by OAuth 2.1 and RFC 9700: it only works for clients with <see cref="Client.AllowImplicitFlow"/> set.
    /// </summary>
    public const string Implicit = "implicit";

    /// <summary>
    /// The resource owner password credentials grant (<c>grant_type=password</c>). Deprecated by OAuth 2.1 and
    /// RFC 9700: it only works for clients with <see cref="Client.AllowResourceOwnerPasswordFlow"/> set.
    /// </summary>
    public const string ResourceOwnerPassword = "password";

    public const string Hybrid = "hybrid";
    public const string AuthorizationCode = "authorization_code";
    public const string ClientCredentials = "client_credentials";
    public const string DeviceFlow = "urn:ietf:params:oauth:grant-type:device_code";
    public const string NoncePattern = "urn:auth:nonce";
}
