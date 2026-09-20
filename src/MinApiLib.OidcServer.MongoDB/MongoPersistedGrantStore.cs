namespace MinApiLib.OidcServer.MongoDB;

/// <summary>
/// Persisted grant store backed by the <c>persistedGrants</c> collection.
/// </summary>
public class MongoPersistedGrantStore : IPersistedGrantStore
{
    private readonly OidcServerMongoContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoPersistedGrantStore"/> class.
    /// </summary>
    public MongoPersistedGrantStore(OidcServerMongoContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task StoreAsync(PersistedGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        return _context.PersistedGrants.ReplaceOneAsync(
            g => g.Id == grant.Key,
            PersistedGrantDocument.From(grant),
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc />
    public async Task<PersistedGrant?> GetAsync(string key)
    {
        var document = await _context.PersistedGrants.Find(g => g.Id == key).FirstOrDefaultAsync();
        return document?.ToModel();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        filter.Validate();
        var documents = await _context.PersistedGrants.Find(Filter(filter)).ToListAsync();
        return documents.Select(d => d.ToModel()).ToList();
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
        => _context.PersistedGrants.DeleteOneAsync(g => g.Id == key);

    /// <inheritdoc />
    public Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        filter.Validate();
        return _context.PersistedGrants.DeleteManyAsync(Filter(filter));
    }

    private static FilterDefinition<PersistedGrantDocument> Filter(PersistedGrantFilter filter)
    {
        var builder = Builders<PersistedGrantDocument>.Filter;
        var conditions = new List<FilterDefinition<PersistedGrantDocument>>();

        if (!string.IsNullOrWhiteSpace(filter.ClientId))
        {
            conditions.Add(builder.Eq(g => g.ClientId, filter.ClientId));
        }

        if (!string.IsNullOrWhiteSpace(filter.SessionId))
        {
            conditions.Add(builder.Eq(g => g.SessionId, filter.SessionId));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubjectId))
        {
            conditions.Add(builder.Eq(g => g.SubjectId, filter.SubjectId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            conditions.Add(builder.Eq(g => g.Type, filter.Type));
        }

        return builder.And(conditions);
    }
}
