namespace MinApiLib.OidcServer.Configuration;

/// <summary>
/// Options for automatic signing key management and rotation.
/// </summary>
public class KeyManagementOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether signing keys are created and rotated automatically. Defaults to
    /// <c>false</c>, so signing keys come only from <c>AddSigningCredential</c> and similar calls. When enabled,
    /// automatic keys sign tokens for <see cref="SigningAlgorithms"/>; static credentials are still published for
    /// validation and sign for any other algorithm.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the algorithms automatic keys are created for. The first one is the default. Defaults to RS256.
    /// </summary>
    public ICollection<string> SigningAlgorithms { get; set; } = new List<string> { SecurityAlgorithms.RsaSha256 };

    /// <summary>
    /// Gets or sets how long a key is used for signing, counted from its creation. Defaults to 90 days.
    /// </summary>
    public TimeSpan RotationInterval { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Gets or sets how long a new key is published in the JWKS before it signs tokens, so clients and APIs that
    /// cache the key set pick it up first. Defaults to 14 days.
    /// </summary>
    public TimeSpan PropagationTime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Gets or sets how long a retired key stays published for validating tokens it signed. Defaults to 14 days.
    /// </summary>
    public TimeSpan RetentionDuration { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Gets or sets a value indicating whether keys past their retention are deleted from the store. Defaults to <c>true</c>.
    /// </summary>
    public bool DeleteRetiredKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets how long keys loaded from the store are cached in memory. Other instances see a new key within
    /// this time, which must be shorter than <see cref="PropagationTime"/>. Defaults to 1 hour.
    /// </summary>
    public TimeSpan KeyCacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets a value indicating whether private keys are encrypted with ASP.NET Core data protection before
    /// they are stored. Defaults to <c>true</c>.
    /// </summary>
    public bool DataProtectKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets the directory used by the default file system key store. Defaults to a <c>keys</c> folder in the
    /// current directory.
    /// </summary>
    public string KeyPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "keys");
}
