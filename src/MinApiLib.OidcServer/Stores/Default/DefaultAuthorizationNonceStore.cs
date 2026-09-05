namespace MinApiLib.OidcServer.Stores;

/// <summary>
/// Default authorization nonce store.
/// </summary>
[RegisterInIServiceCollection(typeof(IAuthorizationNonceStore))]
public class DefaultAuthorizationNonceStore : DefaultGrantStore<AuthorizationNonce>, IAuthorizationNonceStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuthorizationNonceStore"/> class.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="handleGenerationService">The handle generation service.</param>
    /// <param name="logger">The logger.</param>
    public DefaultAuthorizationNonceStore(
        IPersistedGrantStore store,
        IPersistentGrantSerializer serializer,
        IHandleGenerationService handleGenerationService,
        ILogger<DefaultAuthorizationNonceStore> logger)
        : base(PersistedGrantTypes.AuthorizationNonce, store, serializer, handleGenerationService, logger)
    {
    }

    /// <summary>
    /// Stores the authorization nonce asynchronous.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    public Task<string> StoreAuthorizationNonceAsync(AuthorizationNonce nonce)
    {
        return CreateItemAsync(nonce, nonce.ClientId, nonce.Subject.GetSubjectId(), nonce.SessionId, nonce.Description, nonce.CreationTime, nonce.Lifetime);
    }

    /// <summary>
    /// Gets the authorization nonce asynchronous.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    public Task<AuthorizationNonce?> GetAuthorizationNonceAsync(string nonce)
    {
        return GetItemAsync(nonce);
    }

    /// <summary>
    /// Removes the authorization nonce asynchronous.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    public Task RemoveAuthorizationNonceAsync(string nonce)
    {
        return RemoveItemAsync(nonce);
    }
}
