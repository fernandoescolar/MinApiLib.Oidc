namespace MinApiLib.Oidc.Server.Features.Tokens;

/// <summary>
/// Models a validated request to the token endpoint.
/// </summary>
public class ValidatedTokenRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the type of the grant.
    /// </summary>
    /// <value>
    /// The type of the grant.
    /// </value>
    public string GrantType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user name of a password grant request.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the scopes.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public IEnumerable<string> RequestedScopes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the resource indicators (RFC 8707) that determine the audience of the access token.
    /// </summary>
    public IEnumerable<string>? RequestedResourceIndicators { get; set; }

    /// <summary>
    /// Gets or sets the JWK thumbprint of the DPoP proof key (RFC 9449), or <c>null</c> when no proof was sent.
    /// The issued access token is bound to this key.
    /// </summary>
    public string? DPoPKeyThumbprint { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    /// <value>
    /// The refresh token.
    /// </value>
    public RefreshToken? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token handle.
    /// </summary>
    /// <value>
    /// The refresh token handle.
    /// </value>
    public string? RefreshTokenHandle { get; set; }

    /// <summary>
    /// Gets or sets the authorization code.
    /// </summary>
    /// <value>
    /// The authorization code.
    /// </value>
    public AuthorizationCode? AuthorizationCode { get; set; }

    /// <summary>
    /// Gets or sets the authorization code handle.
    /// </summary>
    /// <value>
    /// The authorization code handle.
    /// </value>
    public string? AuthorizationCodeHandle { get; set; }

    /// <summary>
    /// Gets or sets the code verifier.
    /// </summary>
    /// <value>
    /// The code verifier.
    /// </value>
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// Gets or sets the device code.
    /// </summary>
    /// <value>
    /// The device code.
    /// </value>
    public DeviceCode? DeviceCode { get; set; }
}
