namespace IdentityServer.Models;

/// <summary>
/// Models a nonce token for nonce authorization pattern.
/// </summary>
public class AuthorizationNonce
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
    /// Gets or sets the session identifier.
    /// </summary>
    /// <value>
    /// The session identifier.
    /// </value>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public ClaimsPrincipal Subject { get; set; } = default!;

    /// <summary>
    /// Gets or sets the nonce.
    /// </summary>
    /// <value>
    /// The nonce.
    /// </value>
    public string Nonce { get; set; } = default!;

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
}
