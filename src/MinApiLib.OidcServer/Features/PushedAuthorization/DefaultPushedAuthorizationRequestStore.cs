namespace MinApiLib.OidcServer.Features.PushedAuthorization;

/// <summary>
/// Stores pushed authorization requests (RFC 9126) in the persisted grant store.
/// </summary>
[RegisterInIServiceCollection(typeof(IPushedAuthorizationRequestStore))]
public class DefaultPushedAuthorizationRequestStore : DefaultGrantStore<PushedAuthorizationRequest>, IPushedAuthorizationRequestStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPushedAuthorizationRequestStore"/> class.
    /// </summary>
    public DefaultPushedAuthorizationRequestStore(
        IPersistedGrantStore store,
        IPersistentGrantSerializer serializer,
        IHandleGenerationService handleGenerationService,
        ILogger<DefaultPushedAuthorizationRequestStore> logger)
        : base(PersistedGrantTypes.PushedAuthorizationRequest, store, serializer, handleGenerationService, logger)
    {
    }

    /// <inheritdoc/>
    public Task<string> StorePushedAuthorizationRequestAsync(PushedAuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreateItemAsync(request, request.ClientId, null, null, null, request.CreationTime, request.Lifetime);
    }

    /// <inheritdoc/>
    public Task<PushedAuthorizationRequest?> GetPushedAuthorizationRequestAsync(string handle)
    {
        return GetItemAsync(handle);
    }

    /// <inheritdoc/>
    public Task UpdatePushedAuthorizationRequestAsync(string handle, PushedAuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StoreItemAsync(handle, request, request.ClientId, null, null, null, request.CreationTime, request.CreationTime.AddSeconds(request.Lifetime));
    }

    /// <inheritdoc/>
    public Task RemovePushedAuthorizationRequestAsync(string handle)
    {
        return RemoveItemAsync(handle);
    }
}
