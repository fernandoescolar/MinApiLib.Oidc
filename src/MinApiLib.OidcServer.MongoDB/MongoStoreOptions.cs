namespace MinApiLib.OidcServer.MongoDB;

/// <summary>
/// Options of the MongoDB stores.
/// </summary>
public sealed class MongoStoreOptions
{
    /// <summary>
    /// The connection string. Leave it empty to use an <see cref="IMongoClient"/> registered by the host.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The database name. Defaults to <c>oidcserver</c>.
    /// </summary>
    public string DatabaseName { get; set; } = "oidcserver";

    /// <summary>
    /// A prefix for the collection names, for databases shared with other applications.
    /// </summary>
    public string CollectionPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether to store clients, identity resources, API resources, API scopes and CORS origins in MongoDB.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseForConfiguration { get; set; } = true;

    /// <summary>
    /// Whether to store persisted grants (codes, refresh and reference tokens, consents, pushed requests, nonces)
    /// and device codes in MongoDB. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseForOperationalData { get; set; } = true;

    /// <summary>
    /// Whether to store the signing keys of automatic key management in MongoDB. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseForSigningKeys { get; set; } = true;

    /// <summary>
    /// Whether to create the indexes at startup. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The TTL indexes on <c>Expiration</c> let MongoDB delete expired grants and device codes. MongoDB checks them
    /// about once a minute, and the server rejects expired entries until then.
    /// </remarks>
    public bool CreateIndexes { get; set; } = true;
}
