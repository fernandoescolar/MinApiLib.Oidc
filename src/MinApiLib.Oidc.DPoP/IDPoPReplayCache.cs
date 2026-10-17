namespace MinApiLib.Oidc.DPoP;

/// <summary>
/// Remembers the proofs a server has accepted, so each is accepted once (RFC 9449 section 11.1).
/// </summary>
public interface IDPoPReplayCache
{
    /// <summary>Records a proof until it expires.</summary>
    /// <param name="key">The proof: its key thumbprint and <c>jti</c>.</param>
    /// <param name="expiresAt">When the proof can no longer be accepted anyway, and can be forgotten.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="false"/> when the proof was already recorded: a replay.</returns>
    ValueTask<bool> TryAddAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}

/// <summary>
/// An <see cref="IDPoPReplayCache"/> in memory, for a single instance. Use a shared cache when the server runs on
/// several instances, or a proof accepted by one could be replayed against another.
/// </summary>
/// <param name="time">The clock.</param>
public sealed class InMemoryDPoPReplayCache(TimeProvider time) : IDPoPReplayCache
{
    private const int PurgeEvery = 1024;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _proofs = new(StringComparer.Ordinal);
    private int _adds;

    /// <inheritdoc/>
    public ValueTask<bool> TryAddAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var now = time.GetUtcNow();
        if (Interlocked.Increment(ref _adds) % PurgeEvery == 0)
        {
            foreach (var (proof, expiry) in _proofs)
            {
                if (expiry <= now)
                {
                    _proofs.TryRemove(new KeyValuePair<string, DateTimeOffset>(proof, expiry));
                }
            }
        }

        if (_proofs.TryAdd(key, expiresAt))
        {
            return ValueTask.FromResult(true);
        }

        // Recorded before: a replay, unless that record has expired.
        var recorded = _proofs.GetValueOrDefault(key);
        return ValueTask.FromResult(recorded <= now && _proofs.TryUpdate(key, expiresAt, recorded));
    }
}
