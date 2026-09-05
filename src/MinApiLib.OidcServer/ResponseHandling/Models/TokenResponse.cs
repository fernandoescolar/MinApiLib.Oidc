namespace MinApiLib.OidcServer.ResponseHandling;

/// <summary>
/// Models a token response
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Gets or sets the identity token.
    /// </summary>
    /// <value>
    /// The identity token.
    /// </value>
    public string? IdentityToken { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    /// <value>
    /// The access token.
    /// </value>
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the access token type: <c>Bearer</c>, or <c>DPoP</c> for a DPoP-bound token (RFC 9449 section 5).
    /// </summary>
    public string AccessTokenType { get; set; } = OidcConstants.TokenResponse.BearerTokenType;

    /// <summary>
    /// Gets or sets the access token lifetime.
    /// </summary>
    /// <value>
    /// The access token lifetime.
    /// </value>
    public int AccessTokenLifetime { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    /// <value>
    /// The refresh token.
    /// </value>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the scope.
    /// </summary>
    /// <value>
    /// The scope.
    /// </value>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the custom entries.
    /// </summary>
    /// <value>
    /// The custom entries.
    /// </value>
    public Dictionary<string, object>? Custom { get; set; } = new Dictionary<string, object>();
}
