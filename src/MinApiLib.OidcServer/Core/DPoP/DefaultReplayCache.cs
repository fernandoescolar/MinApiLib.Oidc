namespace MinApiLib.OidcServer.Core.DPoP;

/// <summary>
/// Default implementation of the replay cache using IDistributedCache
/// </summary>
[RegisterInIServiceCollection(typeof(IReplayCache))]
public class DefaultReplayCache : IReplayCache
{
    private const string Prefix = nameof(DefaultReplayCache) + ":";

    private readonly IDistributedCache _cache;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="cache"></param>
    public DefaultReplayCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public Task AddAsync(string purpose, string handle, DateTimeOffset expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        };

        return _cache.SetAsync(Prefix + purpose + handle, [], options);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string purpose, string handle)
    {
        return (await _cache.GetAsync(Prefix + purpose + handle, default)) != null;
    }
}
