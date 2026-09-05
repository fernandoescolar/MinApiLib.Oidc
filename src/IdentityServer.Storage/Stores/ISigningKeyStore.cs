namespace IdentityServer.Stores;

/// <summary>
/// Persists the signing keys of automatic key management. Every IdentityServer instance must share it.
/// </summary>
public interface ISigningKeyStore
{
    /// <summary>
    /// Loads all keys.
    /// </summary>
    Task<IEnumerable<SerializedKey>> LoadKeysAsync();

    /// <summary>
    /// Stores a new key.
    /// </summary>
    /// <param name="key">The key.</param>
    Task StoreKeyAsync(SerializedKey key);

    /// <summary>
    /// Deletes a key.
    /// </summary>
    /// <param name="id">The key identifier.</param>
    Task DeleteKeyAsync(string id);
}
