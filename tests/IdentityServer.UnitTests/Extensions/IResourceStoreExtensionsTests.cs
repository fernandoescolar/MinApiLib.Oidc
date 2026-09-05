namespace IdentityServer.UnitTests.Extensions;

public class IResourceStoreExtensionsTests
{
    [Fact]
    public async Task GetAllEnabledResourcesAsync_on_duplicate_identity_scopes_should_fail()
    {
        var store = new MockResourceStore()
        {
            IdentityResources = {
                    new IdentityResource { Name = "A" },
                    new IdentityResource { Name = "A" } }
        };

        var a = () => store.GetAllEnabledResourcesAsync();
        var ex = await Assert.ThrowsAnyAsync<Exception>(a);
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("identity scopes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public Task GetAllEnabledResourcesAsync_without_duplicate_identity_scopes_should_succeed()
    {
        var store = new MockResourceStore()
        {
            IdentityResources = {
                    new IdentityResource { Name = "A" },
                    new IdentityResource { Name = "B" } }
        };

        return store.GetAllEnabledResourcesAsync();
    }

    [Fact]
    public async Task GetAllEnabledResourcesAsync_on_duplicate_api_resources_should_fail()
    {
        var store = new MockResourceStore()
        {
            ApiResources = { new ApiResource { Name = "a" }, new ApiResource { Name = "a" } }
        };

        var a = () => store.GetAllEnabledResourcesAsync();
        var ex = await Assert.ThrowsAnyAsync<Exception>(a);
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api resources", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public Task GetAllEnabledResourcesAsync_without_duplicate_api_scopes_should_succeed()
    {
        var store = new MockResourceStore()
        {
            ApiResources = { new ApiResource("A"), new ApiResource("B") }
        };

        return store.GetAllEnabledResourcesAsync();
    }

    [Fact]
    public async Task FindResourcesByScopeAsync_on_duplicate_identity_scopes_should_fail()
    {
        var store = new MockResourceStore()
        {
            IdentityResources = {
                    new IdentityResource { Name = "A" },
                    new IdentityResource { Name = "A" } }
        };

        var a = () => store.FindResourcesByScopeAsync(new string[] { "A" });
        var ex = await Assert.ThrowsAnyAsync<Exception>(a);
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("identity scopes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public Task FindResourcesByScopeAsync_without_duplicate_identity_scopes_should_succeed()
    {
        var store = new MockResourceStore()
        {
            IdentityResources = {
                    new IdentityResource { Name = "A" },
                    new IdentityResource { Name = "B" } }
        };

        return store.FindResourcesByScopeAsync(new string[] { "A" });
    }

    [Fact]
    public async Task FindResourcesByScopeAsync_on_duplicate_api_scopes_should_succeed()
    {
        var store = new MockResourceStore()
        {
            ApiResources = {
                    new ApiResource { Name = "api1", Scopes = { "a" } },
                    new ApiResource() { Name = "api2", Scopes = { "a" } },
                },
            ApiScopes = {
                    new ApiScope("a")
                }
        };

        var result = await store.FindResourcesByScopeAsync(new string[] { "a" });
        Assert.Equal(2, result.ApiResources.Count);
        Assert.Single(result.ApiScopes);
        Assert.Equivalent(new[] { "api1", "api2" }, result.ApiResources.Select(x => x.Name), strict: true);
        Assert.Equivalent(new[] { "a" }, result.ApiScopes.Select(x => x.Name), strict: true);
    }

    [Fact]
    public Task FindResourcesByScopeAsync_without_duplicate_api_scopes_should_succeed()
    {
        var store = new MockResourceStore()
        {
            ApiResources = { new ApiResource("A"), new ApiResource("B") }
        };

        return store.FindResourcesByScopeAsync(new string[] { "A" });
    }

    [Fact]
    public async Task FindResourcesByScopeAsync_with_duplicate_api_scopes_on_single_api_resource_should_succeed_and_only_reuturn_one_resource()
    {
        var store = new MockResourceStore()
        {
            ApiResources = {
                    new ApiResource {
                        Name = "api1", Scopes = { "a", "a" }
                    }
                },
            ApiScopes = {
                    new ApiScope("a"),
                }
        };

        var result = await store.FindResourcesByScopeAsync(new string[] { "a" });
        Assert.Single(result.ApiResources);
    }

    public class MockResourceStore : IResourceStore
    {
        public List<IdentityResource> IdentityResources { get; set; } = new List<IdentityResource>();
        public List<ApiResource> ApiResources { get; set; } = new List<ApiResource>();
        public List<ApiScope> ApiScopes { get; set; } = new List<ApiScope>();

        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> names)
        {
            var apis = from a in ApiResources
                       where names.Contains(a.Name)
                       select a;
            return Task.FromResult(apis);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var api = from a in ApiResources
                      where a.Scopes.Any(x => names.Contains(x))
                      select a;

            return Task.FromResult(api);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var identity = from i in IdentityResources
                           where names.Contains(i.Name)
                           select i;

            return Task.FromResult(identity);
        }

        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var q = from x in ApiScopes
                    where scopeNames.Contains(x.Name)
                    select x;
            return Task.FromResult(q);
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            var result = new Resources(IdentityResources, ApiResources, ApiScopes);
            return Task.FromResult(result);
        }
    }
}
