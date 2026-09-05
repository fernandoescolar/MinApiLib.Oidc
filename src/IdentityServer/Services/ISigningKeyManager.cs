namespace IdentityServer.Services;

/// <summary>
/// Provides automatically managed signing keys (see <see cref="KeyManagementOptions"/>).
/// </summary>
public interface ISigningKeyManager
{
    /// <summary>
    /// Gets a value indicating whether automatic key management is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// The credential that currently signs for each configured algorithm.
    /// </summary>
    Task<IReadOnlyList<SigningCredentials>> GetSigningCredentialsAsync();

    /// <summary>
    /// Every key that must be published for validation: announced, active and retained keys.
    /// </summary>
    Task<IReadOnlyList<SecurityKeyInfo>> GetValidationKeysAsync();
}
