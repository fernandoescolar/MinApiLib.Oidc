namespace MinApiLib.OidcServer.Features.PushedAuthorization;

/// <summary>
/// Authorize request parameters pushed by a client to the pushed authorization request endpoint (RFC 9126).
/// </summary>
public class PushedAuthorizationRequest
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Gets or sets the lifetime in seconds.
    /// </summary>
    public int Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the client that pushed the request.
    /// </summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the pushed authorize request parameters.
    /// </summary>
    public IDictionary<string, string[]> Parameters { get; set; } = new Dictionary<string, string[]>();
}
