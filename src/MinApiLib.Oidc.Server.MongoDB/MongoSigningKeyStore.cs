namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// Store of the signing keys of automatic key management, backed by the <c>signingKeys</c> collection. All the server
/// instances that share the database share the keys.
/// </summary>
public class MongoSigningKeyStore : ISigningKeyStore
{
    private readonly OidcServerMongoContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoSigningKeyStore"/> class.
    /// </summary>
    public MongoSigningKeyStore(OidcServerMongoContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerializedKey>> LoadKeysAsync()
    {
        var documents = await _context.SigningKeys.Find(FilterDefinition<SigningKeyDocument>.Empty).ToListAsync();
        return documents.Select(d => d.ToModel()).ToList();
    }

    /// <inheritdoc />
    public Task StoreKeyAsync(SerializedKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _context.SigningKeys.ReplaceOneAsync(k => k.Id == key.Id, SigningKeyDocument.From(key), new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc />
    public Task DeleteKeyAsync(string id)
        => _context.SigningKeys.DeleteOneAsync(k => k.Id == id);
}
