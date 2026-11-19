namespace IdP.Users;

/// <summary>
/// The collections of the identity provider that are not part of the OpenID Connect server's stores: users, roles,
/// groups, external identity providers, audit events and data protection keys. They live in the same database as
/// <see cref="OidcServerMongoContext"/>.
/// </summary>
public sealed class IdPMongoContext
{
    public IdPMongoContext(IMongoDatabase database)
    {
        Database = database;
        Users = database.GetCollection<IdPUser>("users");
        Roles = database.GetCollection<IdPRole>("roles");
        Groups = database.GetCollection<IdPGroup>("groups");
        IdentityProviders = database.GetCollection<IdentityProviderDocument>("identityProviders");
        Events = database.GetCollection<EventDocument>("events");
        DataProtectionKeys = database.GetCollection<MongoXmlRepository.KeyDocument>("dataProtectionKeys");
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<IdPUser> Users { get; }

    public IMongoCollection<IdPRole> Roles { get; }

    public IMongoCollection<IdPGroup> Groups { get; }

    public IMongoCollection<IdentityProviderDocument> IdentityProviders { get; }

    public IMongoCollection<EventDocument> Events { get; }

    public IMongoCollection<MongoXmlRepository.KeyDocument> DataProtectionKeys { get; }

    public async Task CreateIndexesAsync(TimeSpan eventsRetention, CancellationToken cancellationToken)
    {
        await Users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending(u => u.NormalizedUserName), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending(u => u.NormalizedEmail), new CreateIndexOptions<IdPUser>
            {
                Unique = true,
                PartialFilterExpression = Builders<IdPUser>.Filter.Type(u => u.NormalizedEmail, BsonType.String)
            }),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending("Logins.LoginProvider").Ascending("Logins.ProviderKey")),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending("Passkeys.CredentialId")),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending(u => u.Roles)),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Ascending(u => u.Groups)),
            new CreateIndexModel<IdPUser>(Builders<IdPUser>.IndexKeys.Descending(u => u.CreatedAt))
        ], cancellationToken);

        await Roles.Indexes.CreateOneAsync(
            new CreateIndexModel<IdPRole>(Builders<IdPRole>.IndexKeys.Ascending(r => r.NormalizedName), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await Groups.Indexes.CreateOneAsync(
            new CreateIndexModel<IdPGroup>(Builders<IdPGroup>.IndexKeys.Ascending(g => g.Name), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        // The TTL index deletes old events; its expiry is changed in place when the retention changes.
        var ttl = new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Ascending(e => e.TimeStamp),
            new CreateIndexOptions { ExpireAfter = eventsRetention, Name = "TimeStamp_ttl" });
        try
        {
            await Events.Indexes.CreateOneAsync(ttl, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.Code == 85 || ex.Code == 86)
        {
            await Events.Indexes.DropOneAsync("TimeStamp_ttl", cancellationToken);
            await Events.Indexes.CreateOneAsync(ttl, cancellationToken: cancellationToken);
        }

        await Events.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Descending(e => e.TimeStamp).Ascending(e => e.Category)),
            new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Ascending(e => e.SubjectId).Descending(e => e.TimeStamp)),
            new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Ascending(e => e.ClientId).Descending(e => e.TimeStamp))
        ], cancellationToken);
    }
}
