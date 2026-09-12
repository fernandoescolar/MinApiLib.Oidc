namespace MinApiLib.OidcServer.Core.Stores;

/// <summary>
/// Cache decorator for IClientStore
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="MinApiLib.OidcServer.Core.Stores.IClientStore" />
public class CachingClientStore<T> : IClientStore
    where T : IClientStore
{
    private readonly OidcServerOptions _options;
    private readonly ICache<Client> _cache;
    private readonly IClientStore _inner;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingClientStore{T}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="inner">The inner.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="logger">The logger.</param>
    public CachingClientStore(OidcServerOptions options, T inner, ICache<Client> cache, ILogger<CachingClientStore<T>> logger)
    {
        _options = options;
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Finds a client by id
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <returns>
    /// The client
    /// </returns>
    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        var client = await _cache.GetAsync(clientId,
            _options.Caching.ClientStoreExpiration,
            async () => await _inner.FindClientByIdAsync(clientId),
            _logger);

        return client;
    }
}
