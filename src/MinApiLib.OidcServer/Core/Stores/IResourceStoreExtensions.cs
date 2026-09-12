namespace MinApiLib.OidcServer.Core.Stores;

/// <summary>
/// Extensions for IResourceStore
/// </summary>
public static class IResourceStoreExtensions
{
    /// <summary>
    /// Finds the resources by scope.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="scopeNames">The scope names.</param>
    /// <returns></returns>
    public static async Task<Resources> FindResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
    {
        var identity = await store.FindIdentityResourcesByScopeNameAsync(scopeNames);
        var apiResources = await store.FindApiResourcesByScopeNameAsync(scopeNames);
        var scopes = await store.FindApiScopesByNameAsync(scopeNames);

        Validate(identity, apiResources, scopes);

        var resources = new Resources(identity, apiResources, scopes)
        {
            OfflineAccess = scopeNames.Contains(OidcServerConstants.StandardScopes.OfflineAccess)
        };

        return resources;
    }

    private static void Validate(IEnumerable<IdentityResource> identity, IEnumerable<ApiResource> apiResources, IEnumerable<ApiScope> apiScopes)
    {
        var identityScopeNames = identity.Select(x => x.Name).ToArray();
        var dups = GetDuplicates(identityScopeNames);
        if (dups.Length > 0)
        {
            var names = dups.Aggregate((x, y) => x + ", " + y);
            throw new InvalidOperationException(
                $"Duplicate identity scopes found. This is an invalid configuration. Use different names for identity scopes. Scopes found: {names}");
        }

        var apiNames = apiResources.Select(x => x.Name);
        dups = GetDuplicates(apiNames);
        if (dups.Length > 0)
        {
            var names = dups.Aggregate((x, y) => x + ", " + y);
            throw new InvalidOperationException(
                $"Duplicate api resources found. This is an invalid configuration. Use different names for API resources. Names found: {names}");
        }

        var scopesNames = apiScopes.Select(x => x.Name);
        dups = GetDuplicates(scopesNames);
        if (dups.Length > 0)
        {
            var names = dups.Aggregate((x, y) => x + ", " + y);
            throw new InvalidOperationException(
                $"Duplicate scopes found. This is an invalid configuration. Use different names for scopes. Names found: {names}");
        }

        var overlap = identityScopeNames.Intersect(scopesNames).ToArray();
        if (overlap.Length > 0)
        {
            var names = overlap.Aggregate((x, y) => x + ", " + y);
            throw new InvalidOperationException(
                $"Found identity scopes and API scopes that use the same names. This is an invalid configuration. Use different names for identity scopes and API scopes. Scopes found: {names}");
        }
    }

    private static string[] GetDuplicates(IEnumerable<string> names)
        => names.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToArray();

    /// <summary>
    /// Finds the enabled resources by scope.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="scopeNames">The scope names.</param>
    /// <returns></returns>
    public static async Task<Resources> FindEnabledResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
    {
        return (await store.FindResourcesByScopeAsync(scopeNames)).FilterEnabled();
    }

    /// <summary>
    /// Creates a resource validation result.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="parsedScopesResult">The parsed scopes.</param>
    /// <returns></returns>
    public static async Task<ResourceValidationResult> CreateResourceValidationResult(this IResourceStore store, ParsedScopesResult parsedScopesResult)
    {
        var validScopeValues = parsedScopesResult.ParsedScopes;
        var scopes = validScopeValues.Select(x => x.ParsedName).ToArray();
        var resources = await store.FindEnabledResourcesByScopeAsync(scopes);
        return new ResourceValidationResult(resources, validScopeValues);
    }

    /// <summary>
    /// Gets all enabled resources.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns></returns>
    public static async Task<Resources> GetAllEnabledResourcesAsync(this IResourceStore store)
    {
        var resources = await store.GetAllResourcesAsync();
        Validate(resources.IdentityResources, resources.ApiResources, resources.ApiScopes);

        return resources.FilterEnabled();
    }

    /// <summary>
    /// Finds the enabled identity resources by scope.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="scopeNames">The scope names.</param>
    /// <returns></returns>
    public static async Task<IEnumerable<IdentityResource>> FindEnabledIdentityResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
    {
        return (await store.FindIdentityResourcesByScopeNameAsync(scopeNames)).Where(x => x.Enabled).ToArray();
    }
}
