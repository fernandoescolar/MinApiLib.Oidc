namespace MinApiLib.Oidc.Http.ClientCredentials;

/// <summary>
/// A client that gets its own access tokens with the client credentials grant (RFC 6749 section 4.4).
/// </summary>
public sealed class ClientCredentialsOptions
{
    /// <summary>
    /// Gets or sets the URL of the server. Its discovery document gives the token endpoint. Set this or
    /// <see cref="TokenEndpoint"/>.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>Gets or sets the URL of the token endpoint, to skip discovery.</summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>Gets or sets whether the discovery document must be read over HTTPS. Defaults to <see langword="true"/>.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Gets or sets the client id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the client secret, sent in the request body (<c>client_secret_post</c>).</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Gets or sets the scopes to ask for, separated by spaces.</summary>
    public string? Scope { get; set; }

    /// <summary>Gets more parameters of the token request, such as <c>resource</c>.</summary>
    public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets whether to ask for DPoP-bound tokens: the token request and every API call carry a proof of the
    /// client's key. Defaults to <see langword="false"/>: bearer tokens.
    /// </summary>
    public bool UseDPoP { get; set; }

    /// <summary>
    /// Gets or sets the client's DPoP key, as a private JSON Web Key (see <see cref="DPoPKey.ToJwk"/>). When it is not
    /// set, the client creates an EC P-256 key in memory, and a new one when the application restarts.
    /// </summary>
    public string? DPoPJsonWebKey { get; set; }

    /// <summary>
    /// Gets or sets how long before it expires a token is replaced by a new one. Defaults to 1 minute.
    /// </summary>
    public TimeSpan RenewBeforeExpiration { get; set; } = TimeSpan.FromMinutes(1);
}
