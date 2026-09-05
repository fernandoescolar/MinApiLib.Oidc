namespace MinApiLib.OidcServer.Models;

/// <summary>
/// A signing key created by automatic key management, in its stored form.
/// </summary>
public class SerializedKey
{
    /// <summary>
    /// Gets or sets the format version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the key identifier, published as <c>kid</c>.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the creation time (UTC).
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets the signing algorithm.
    /// </summary>
    public string Algorithm { get; set; } = default!;

    /// <summary>
    /// Gets or sets the PKCS#8 private key, base64 encoded, possibly protected with data protection.
    /// </summary>
    public string Data { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Data"/> is protected with data protection.
    /// </summary>
    public bool DataProtected { get; set; }
}
