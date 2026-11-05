namespace MinApiLib.Oidc.Server.IntegrationTests.Stores;

/// <summary>
/// The in-memory stores remove expired entries when a new one is stored, at most once a minute.
/// </summary>
public class InMemoryStoreTests
{
    private const string Category = "In-memory stores";

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly MutableClock _clock = new();

    [Fact]
    [Trait("Category", Category)]
    public async Task Storing_a_grant_removes_the_expired_ones()
    {
        var store = new InMemoryPersistedGrantStore(_clock);
        await store.StoreAsync(Grant("expiring", _clock.Now.AddMinutes(5)));
        await store.StoreAsync(Grant("lasting", _clock.Now.AddHours(1)));
        await store.StoreAsync(Grant("forever", null));

        _clock.Now = _clock.Now.AddMinutes(10);
        await store.StoreAsync(Grant("new", _clock.Now.AddMinutes(5)));

        Assert.Null(await store.GetAsync("expiring"));
        Assert.NotNull(await store.GetAsync("lasting"));
        Assert.NotNull(await store.GetAsync("forever"));
        Assert.NotNull(await store.GetAsync("new"));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Grants_are_purged_at_most_once_a_minute()
    {
        var store = new InMemoryPersistedGrantStore(_clock);
        await store.StoreAsync(Grant("short", _clock.Now.AddSeconds(10)));

        _clock.Now = _clock.Now.AddSeconds(30);
        await store.StoreAsync(Grant("other", _clock.Now.AddHours(1)));
        Assert.NotNull(await store.GetAsync("short"));

        _clock.Now = _clock.Now.AddSeconds(31);
        await store.StoreAsync(Grant("another", _clock.Now.AddHours(1)));
        Assert.Null(await store.GetAsync("short"));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Storing_a_device_authorization_removes_the_expired_ones()
    {
        var store = new InMemoryDeviceFlowStore(_clock);
        await store.StoreDeviceAuthorizationAsync("device-1", "user-1", Device(lifetime: 300));
        await store.StoreDeviceAuthorizationAsync("device-2", "user-2", Device(lifetime: 3600));

        _clock.Now = _clock.Now.AddMinutes(10);
        await store.StoreDeviceAuthorizationAsync("device-3", "user-3", Device(lifetime: 300));

        Assert.Null(await store.FindByDeviceCodeAsync("device-1"));
        Assert.Null(await store.FindByUserCodeAsync("user-1"));
        Assert.NotNull(await store.FindByDeviceCodeAsync("device-2"));
        Assert.NotNull(await store.FindByDeviceCodeAsync("device-3"));
    }

    [Fact]
    [Trait("Category", Category)]
    public void Stores_registered_by_AddOidcServer_use_the_container_clock()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(_clock);
        services.AddLogging();
        services.AddOidcServer();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<InMemoryPersistedGrantStore>(provider.GetRequiredService<IPersistedGrantStore>());
        Assert.IsType<InMemoryDeviceFlowStore>(provider.GetRequiredService<IDeviceFlowStore>());
    }

    private PersistedGrant Grant(string key, DateTimeOffset? expiration) => new()
    {
        Key = key,
        Type = "test",
        ClientId = "client",
        CreationTime = _clock.Now.UtcDateTime,
        Expiration = expiration?.UtcDateTime,
        Data = "{}",
    };

    private DeviceCode Device(int lifetime) => new()
    {
        ClientId = "client",
        CreationTime = _clock.Now.UtcDateTime,
        Lifetime = lifetime,
        RequestedScopes = ["openid"],
    };
}
