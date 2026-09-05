namespace IdentityServer.UnitTests.Stores.Default;

public class DistributedCacheAuthorizationParametersMessageStoreTests
{
    [Fact]
    public async Task Delete_should_remove_the_cached_entry()
    {
        var cache = new IdentityServer.UnitTests.Services.Default.TestCache();
        var store = new IdentityServer.Stores.Default.DistributedCacheAuthorizationParametersMessageStore(cache, new DefaultHandleGenerationService());

        var id = await store.WriteAsync(new Message<IDictionary<string, string[]>>(
            new Dictionary<string, string[]> { { "client_id", ["client"] } }, DateTime.UtcNow));
        Assert.NotEmpty(cache.Items);

        await store.DeleteAsync(id);

        Assert.Empty(cache.Items);
    }
}
