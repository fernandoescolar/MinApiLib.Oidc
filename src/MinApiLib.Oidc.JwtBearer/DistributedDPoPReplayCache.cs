namespace MinApiLib.Oidc.JwtBearer;

/// <summary>
/// An <see cref="IDPoPReplayCache"/> on <see cref="IDistributedCache"/>, such as Redis, for APIs that run on several
/// instances. Register it with <c>services.AddSingleton&lt;IDPoPReplayCache, DistributedDPoPReplayCache&gt;()</c>.
/// </summary>
/// <remarks>
/// <see cref="IDistributedCache"/> has no atomic add: two instances that get the same proof at the same moment may
/// both accept it. The proof is still bound to the key, the request and the access token.
/// </remarks>
/// <param name="cache">The distributed cache.</param>
/// <param name="time">The clock.</param>
public sealed class DistributedDPoPReplayCache(IDistributedCache cache, TimeProvider time) : IDPoPReplayCache
{
    private const string Prefix = "MinApiLib.Oidc.JwtBearer.Replay:";
    private static readonly byte[] Marker = [1];

    /// <inheritdoc/>
    public async ValueTask<bool> TryAddAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (await cache.GetAsync(Prefix + key, cancellationToken) != null)
        {
            return false;
        }

        var lifetime = expiresAt - time.GetUtcNow();
        if (lifetime > TimeSpan.Zero)
        {
            await cache.SetAsync(Prefix + key, Marker, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime }, cancellationToken);
        }

        return true;
    }
}
