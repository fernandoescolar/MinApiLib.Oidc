namespace IdentityServer.Configuration;

/// <summary>
/// Options for pushed authorization requests (RFC 9126).
/// </summary>
public class PushedAuthorizationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether every client must use pushed authorization requests. Individual
    /// clients can also require it with <see cref="Client.RequirePushedAuthorization"/>. Defaults to <c>false</c>.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the lifetime, in seconds, of a pushed authorization request. The request must survive the user
    /// interaction (login and consent) that follows the redirect to the authorize endpoint. Defaults to 600.
    /// </summary>
    public int Lifetime { get; set; } = 600;
}
