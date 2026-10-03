namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// Resource store backed by the <c>identityResources</c>, <c>apiResources</c> and <c>apiScopes</c> collections.
/// </summary>
public class MongoResourceStore : IResourceStore
{
    private readonly OidcServerMongoContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoResourceStore"/> class.
    /// </summary>
    public MongoResourceStore(OidcServerMongoContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        ArgumentNullException.ThrowIfNull(scopeNames);
        var names = scopeNames.ToList();
        return await _context.IdentityResources.Find(Builders<IdentityResource>.Filter.In(r => r.Name, names)).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        ArgumentNullException.ThrowIfNull(scopeNames);
        var names = scopeNames.ToList();
        return await _context.ApiScopes.Find(Builders<ApiScope>.Filter.In(s => s.Name, names)).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        ArgumentNullException.ThrowIfNull(scopeNames);
        var names = scopeNames.ToList();
        return await _context.ApiResources.Find(Builders<ApiResource>.Filter.AnyIn(r => r.Scopes, names)).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        ArgumentNullException.ThrowIfNull(apiResourceNames);
        var names = apiResourceNames.ToList();
        return await _context.ApiResources.Find(Builders<ApiResource>.Filter.In(r => r.Name, names)).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Resources> GetAllResourcesAsync()
    {
        var identity = await _context.IdentityResources.Find(FilterDefinition<IdentityResource>.Empty).ToListAsync();
        var apis = await _context.ApiResources.Find(FilterDefinition<ApiResource>.Empty).ToListAsync();
        var scopes = await _context.ApiScopes.Find(FilterDefinition<ApiScope>.Empty).ToListAsync();

        return new Resources(identity, apis, scopes);
    }
}
