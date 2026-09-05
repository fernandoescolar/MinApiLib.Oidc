namespace IdentityServer.Models;

/// <summary>
/// Models an authorization code.
/// </summary>
public class AuthorizationCode
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    /// <value>
    /// The creation time.
    /// </value>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the life time in seconds.
    /// </summary>
    /// <value>
    /// The life time.
    /// </value>
    public int Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the ID of the client.
    /// </summary>
    /// <value>
    /// The ID of the client.
    /// </value>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public ClaimsPrincipal Subject { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether this code is an OpenID Connect code.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is open identifier; otherwise, <c>false</c>.
    /// </value>
    public bool IsOpenId { get; set; }

    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    /// <value>
    /// The requested scopes.
    /// </value>
    public IEnumerable<string> RequestedScopes { get; set; } = default!;

    /// <summary>
    /// Gets or sets the redirect URI.
    /// </summary>
    /// <value>
    /// The redirect URI.
    /// </value>
    public string RedirectUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the nonce.
    /// </summary>
    /// <value>
    /// The nonce.
    /// </value>
    public string? Nonce { get; set; }

    /// <summary>
    /// Gets or sets the hashed state (to output s_hash claim).
    /// </summary>
    /// <value>
    /// The hashed state.
    /// </value>
    public string? StateHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether consent was shown.
    /// </summary>
    /// <value>
    ///   <c>true</c> if consent was shown; otherwise, <c>false</c>.
    /// </value>
    public bool WasConsentShown { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    /// <value>
    /// The session identifier.
    /// </value>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the code challenge.
    /// </summary>
    /// <value>
    /// The code challenge.
    /// </value>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// Gets or sets the code challenge method.
    /// </summary>
    /// <value>
    /// The code challenge method
    /// </value>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Gets or sets the description the user assigned to this grant, for example the name of the device or application.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets properties
    /// </summary>
    /// <value>
    /// The properties
    /// </value>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the time the code was redeemed. A consumed code is kept until it expires so that a
    /// second redemption can be detected (RFC 6749 section 4.1.2).
    /// </summary>
    public DateTime? ConsumedTime { get; set; }

    /// <summary>
    /// Gets or sets the resource indicators (RFC 8707) requested at the authorize endpoint.
    /// </summary>
    public IEnumerable<string>? RequestedResourceIndicators { get; set; }

    /// <summary>
    /// Gets or sets the JWK thumbprint of the DPoP key the code is bound to (RFC 9449 section 10).
    /// </summary>
    public string? DPoPKeyThumbprint { get; set; }
}
