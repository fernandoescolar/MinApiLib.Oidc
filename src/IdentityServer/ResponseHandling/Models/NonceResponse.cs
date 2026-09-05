namespace IdentityServer.ResponseHandling;

public class NonceResponse
{
    public string Nonce { get; set; } = default!;

    public int Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the custom entries.
    /// </summary>
    /// <value>
    /// The custom entries.
    /// </value>
    public Dictionary<string, object> Custom { get; set; } = new Dictionary<string, object>();
}
