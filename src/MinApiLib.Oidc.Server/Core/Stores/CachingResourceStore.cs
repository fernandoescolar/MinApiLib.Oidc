namespace MinApiLib.Oidc.Server.Core.Stores;

/// <summary>
/// Caching decorator for IResourceStore
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Stores.IResourceStore" />
public class CachingResourceStore<T> : IResourceStore
    where T : IResourceStore
{
    private const string AllKey = "__all__";

    private readonly OidcServerOptions _options;

    private readonly ICache<IEnumerable<IdentityResource>> _identityCache;
    private readonly ICache<IEnumerable<ApiResource>> _apiByScopeCache;
    private readonly ICache<IEnumerable<ApiScope>> _apiScopeCache;
    private readonly ICache<IEnumerable<ApiResource>> _apiResourceCache;
    private readonly ICache<Resources> _allCache;

    private readonly IResourceStore _inner;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingResourceStore{T}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="inner">The inner.</param>
    /// <param name="identityCache">The identity cache.</param>
    /// <param name="apiByScopeCache">The API by scope cache.</param>
    /// <param name="apisCache">The API cache.</param>
    /// <param name="scopeCache"></param>
    /// <param name="allCache">All cache.</param>
    /// <param name="logger">The logger.</param>
    public CachingResourceStore(OidcServerOptions options, T inner,
        ICache<IEnumerable<IdentityResource>> identityCache,
        ICache<IEnumerable<ApiResource>> apiByScopeCache,
        ICache<IEnumerable<ApiResource>> apisCache,
        ICache<IEnumerable<ApiScope>> scopeCache,
        ICache<Resources> allCache,
        ILogger<CachingResourceStore<T>> logger)
    {
        _options = options;
        _inner = inner;
        _identityCache = identityCache;
        _apiByScopeCache = apiByScopeCache;
        _apiResourceCache = apisCache;
        _apiScopeCache = scopeCache;
        _allCache = allCache;
        _logger = logger;
    }

    private string GetKey(IEnumerable<string> names)
    {
        if (names == null || !names.Any()) return string.Empty;
        return names.OrderBy(x => x).Aggregate((x, y) => x + "," + y);
    }

    /// <inheritdoc/>
    public async Task<Resources> GetAllResourcesAsync()
    {
        var key = AllKey;

        var all = await _allCache.GetAsync(key,
            _options.Caching.ResourceStoreExpiration,
            async () => await _inner.GetAllResourcesAsync(),
            _logger);

        // keys are never null and the inner store never returns null, so neither does the cache (here and below)
        return all!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        var key = GetKey(apiResourceNames);

        var apis = await _apiResourceCache.GetAsync(key,
            _options.Caching.ResourceStoreExpiration,
            async () => await _inner.FindApiResourcesByNameAsync(apiResourceNames),
            _logger);

        return apis!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        var key = GetKey(scopeNames);

        var identities = await _identityCache.GetAsync(key,
            _options.Caching.ResourceStoreExpiration,
            async () => await _inner.FindIdentityResourcesByScopeNameAsync(scopeNames),
            _logger);

        return identities!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        var key = GetKey(scopeNames);

        var apis = await _apiByScopeCache.GetAsync(key,
            _options.Caching.ResourceStoreExpiration,
            async () => await _inner.FindApiResourcesByScopeNameAsync(scopeNames),
            _logger);

        return apis!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        var key = GetKey(scopeNames);

        var apis = await _apiScopeCache.GetAsync(key,
            _options.Caching.ResourceStoreExpiration,
            async () => await _inner.FindApiScopesByNameAsync(scopeNames),
            _logger);

        return apis!;
    }
}
