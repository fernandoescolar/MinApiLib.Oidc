namespace MinApiLib.Oidc.JwtBearer.Tests;

public sealed class ReplayCacheTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch.AddYears(56);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task In_memory_cache_accepts_a_proof_once_until_it_expires()
    {
        var clock = new Clock();
        var cache = new InMemoryDPoPReplayCache(clock);
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await cache.TryAddAsync("a", clock.Now.AddMinutes(3), ct));
        Assert.False(await cache.TryAddAsync("a", clock.Now.AddMinutes(3), ct));
        Assert.True(await cache.TryAddAsync("b", clock.Now.AddMinutes(3), ct));

        clock.Now = clock.Now.AddMinutes(4);
        Assert.True(await cache.TryAddAsync("a", clock.Now.AddMinutes(3), ct));
    }

    [Fact]
    public async Task Distributed_cache_accepts_a_proof_once()
    {
        var cache = new DistributedDPoPReplayCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), TimeProvider.System);
        var ct = TestContext.Current.CancellationToken;

        Assert.True(await cache.TryAddAsync("a", DateTimeOffset.UtcNow.AddMinutes(3), ct));
        Assert.False(await cache.TryAddAsync("a", DateTimeOffset.UtcNow.AddMinutes(3), ct));
    }
}
