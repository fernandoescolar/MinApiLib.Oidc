namespace MinApiLib.OidcServer.MongoDB;

/// <summary>
/// The MongoDB collections of the OidcServer stores. Hosts can use it to seed or manage the configuration.
/// </summary>
public sealed class OidcServerMongoContext
{
    private static readonly object MapsLock = new();
    private static bool _mapsRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcServerMongoContext"/> class.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="collectionPrefix">A prefix for the collection names.</param>
    public OidcServerMongoContext(IMongoDatabase database, string collectionPrefix = "")
    {
        ArgumentNullException.ThrowIfNull(database);
        RegisterClassMaps();

        Database = database;
        Clients = database.GetCollection<Client>(collectionPrefix + "clients");
        IdentityResources = database.GetCollection<IdentityResource>(collectionPrefix + "identityResources");
        ApiResources = database.GetCollection<ApiResource>(collectionPrefix + "apiResources");
        ApiScopes = database.GetCollection<ApiScope>(collectionPrefix + "apiScopes");
        PersistedGrants = database.GetCollection<PersistedGrantDocument>(collectionPrefix + "persistedGrants");
        DeviceCodes = database.GetCollection<DeviceFlowDocument>(collectionPrefix + "deviceCodes");
        SigningKeys = database.GetCollection<SigningKeyDocument>(collectionPrefix + "signingKeys");
    }

    /// <summary>
    /// The collation of CORS origin lookups: origins are compared without regard to case.
    /// </summary>
    internal static readonly Collation CaseInsensitive = new("en", strength: CollationStrength.Secondary);

    /// <summary>The database.</summary>
    public IMongoDatabase Database { get; }

    /// <summary>The clients, by <see cref="Client.ClientId"/>.</summary>
    public IMongoCollection<Client> Clients { get; }

    /// <summary>The identity resources, by name.</summary>
    public IMongoCollection<IdentityResource> IdentityResources { get; }

    /// <summary>The API resources, by name.</summary>
    public IMongoCollection<ApiResource> ApiResources { get; }

    /// <summary>The API scopes, by name.</summary>
    public IMongoCollection<ApiScope> ApiScopes { get; }

    /// <summary>The persisted grants, by key.</summary>
    public IMongoCollection<PersistedGrantDocument> PersistedGrants { get; }

    /// <summary>The device codes, by device code.</summary>
    public IMongoCollection<DeviceFlowDocument> DeviceCodes { get; }

    /// <summary>The signing keys, by key id.</summary>
    public IMongoCollection<SigningKeyDocument> SigningKeys { get; }

    /// <summary>
    /// Inserts or replaces a client.
    /// </summary>
    public Task UpsertAsync(Client client, CancellationToken cancellationToken = default)
        => Clients.ReplaceOneAsync(c => c.ClientId == client.ClientId, client, new ReplaceOptions { IsUpsert = true }, cancellationToken);

    /// <summary>
    /// Inserts or replaces an identity resource.
    /// </summary>
    public Task UpsertAsync(IdentityResource resource, CancellationToken cancellationToken = default)
        => IdentityResources.ReplaceOneAsync(r => r.Name == resource.Name, resource, new ReplaceOptions { IsUpsert = true }, cancellationToken);

    /// <summary>
    /// Inserts or replaces an API resource.
    /// </summary>
    public Task UpsertAsync(ApiResource resource, CancellationToken cancellationToken = default)
        => ApiResources.ReplaceOneAsync(r => r.Name == resource.Name, resource, new ReplaceOptions { IsUpsert = true }, cancellationToken);

    /// <summary>
    /// Inserts or replaces an API scope.
    /// </summary>
    public Task UpsertAsync(ApiScope scope, CancellationToken cancellationToken = default)
        => ApiScopes.ReplaceOneAsync(s => s.Name == scope.Name, scope, new ReplaceOptions { IsUpsert = true }, cancellationToken);

    /// <summary>
    /// Creates the indexes the stores rely on, including the TTL indexes that delete expired grants and device codes.
    /// </summary>
    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        await PersistedGrants.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<PersistedGrantDocument>(Builders<PersistedGrantDocument>.IndexKeys
                .Ascending(g => g.SubjectId).Ascending(g => g.ClientId).Ascending(g => g.Type)),
            new CreateIndexModel<PersistedGrantDocument>(Builders<PersistedGrantDocument>.IndexKeys
                .Ascending(g => g.SubjectId).Ascending(g => g.SessionId).Ascending(g => g.Type)),
            new CreateIndexModel<PersistedGrantDocument>(Builders<PersistedGrantDocument>.IndexKeys.Ascending(g => g.Expiration),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "Expiration_ttl" })
        ], cancellationToken);

        await DeviceCodes.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<DeviceFlowDocument>(Builders<DeviceFlowDocument>.IndexKeys.Ascending(d => d.UserCode),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<DeviceFlowDocument>(Builders<DeviceFlowDocument>.IndexKeys.Ascending(d => d.Expiration),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "Expiration_ttl" })
        ], cancellationToken);

        await Clients.Indexes.CreateOneAsync(
            new CreateIndexModel<Client>(Builders<Client>.IndexKeys.Ascending(nameof(Client.AllowedCorsOrigins)),
                new CreateIndexOptions { Collation = CaseInsensitive }),
            cancellationToken: cancellationToken);

        await ApiResources.Indexes.CreateOneAsync(
            new CreateIndexModel<ApiResource>(Builders<ApiResource>.IndexKeys.Ascending(nameof(ApiResource.Scopes))),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Maps the configuration models: the client id and the resource names are the document ids, enums are stored as
    /// strings and unknown fields are ignored, so documents written by a newer version still load.
    /// </summary>
    private static void RegisterClassMaps()
    {
        lock (MapsLock)
        {
            if (_mapsRegistered)
            {
                return;
            }

            ConventionRegistry.Register(
                "MinApiLib.OidcServer",
                new ConventionPack { new EnumRepresentationConvention(BsonType.String), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("MinApiLib.OidcServer", StringComparison.Ordinal) == true);

            TryRegister<Client>(map =>
            {
                map.AutoMap();
                map.MapIdMember(c => c.ClientId);
            });
            TryRegister<Resource>(map =>
            {
                map.AutoMap();
                map.MapIdMember(r => r.Name);
            });
            TryRegister<IdentityResource>(map => map.AutoMap());
            TryRegister<ApiResource>(map => map.AutoMap());
            TryRegister<ApiScope>(map => map.AutoMap());

            _mapsRegistered = true;
        }
    }

    private static void TryRegister<T>(Action<BsonClassMap<T>> map)
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            BsonClassMap.RegisterClassMap(map);
        }
    }
}
