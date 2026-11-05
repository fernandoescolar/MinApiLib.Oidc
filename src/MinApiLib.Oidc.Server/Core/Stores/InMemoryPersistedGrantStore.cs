namespace MinApiLib.Oidc.Server.Core.Stores;

/// <summary>
/// In-memory persisted grant store. Storing a grant also removes the expired ones, at most once a minute.
/// </summary>
public class InMemoryPersistedGrantStore : IPersistedGrantStore
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, PersistedGrant> _repository = new ConcurrentDictionary<string, PersistedGrant>();
    private readonly TimeProvider _clock;
    private long _nextPurgeTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPersistedGrantStore"/> class with the system clock.
    /// </summary>
    public InMemoryPersistedGrantStore()
        : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryPersistedGrantStore"/> class.
    /// </summary>
    /// <param name="clock">The clock that decides which grants have expired.</param>
    public InMemoryPersistedGrantStore(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <inheritdoc/>
    public Task StoreAsync(PersistedGrant grant)
    {
        PurgeExpired();
        _repository[grant.Key] = grant;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PersistedGrant?> GetAsync(string key)
    {
        if (_repository.TryGetValue(key, out PersistedGrant? token))
        {
            return Task.FromResult<PersistedGrant?>(token);
        }

        return Task.FromResult<PersistedGrant?>(null);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        filter.Validate();

        var items = Filter(filter);

        return Task.FromResult(items);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key)
    {
        _repository.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        filter.Validate();

        var items = Filter(filter);

        foreach (var item in items)
        {
            _repository.TryRemove(item.Key, out _);
        }

        return Task.CompletedTask;
    }

    private void PurgeExpired()
    {
        var now = _clock.GetUtcNow();
        var next = Interlocked.Read(ref _nextPurgeTicks);
        if (now.UtcTicks < next
            || Interlocked.CompareExchange(ref _nextPurgeTicks, now.Add(PurgeInterval).UtcTicks, next) != next)
        {
            return;
        }

        foreach (var item in _repository)
        {
            if (item.Value.Expiration < now.UtcDateTime)
            {
                _repository.TryRemove(item);
            }
        }
    }

    private IEnumerable<PersistedGrant> Filter(PersistedGrantFilter filter)
    {
        var query =
            from item in _repository
            select item.Value;

        if (!String.IsNullOrWhiteSpace(filter.ClientId))
        {
            query = query.Where(x => x.ClientId == filter.ClientId);
        }
        if (!String.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }
        if (!String.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }
        if (!String.IsNullOrWhiteSpace(filter.Type))
        {
            query = query.Where(x => x.Type == filter.Type);
        }

        var items = query.ToArray().AsEnumerable();
        return items;
    }
}
