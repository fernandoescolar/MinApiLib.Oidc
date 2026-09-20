namespace MinApiLib.OidcServer.MongoDB.Tests;

public class OperationalStoreTests(MongoFixture mongo)
{
    private static readonly DateTime Now = new(2027, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    private readonly OidcServerMongoContext _context = mongo.CreateContext();

    private static PersistedGrant Grant(string key, string subject = "bob", string client = "web", string type = "refresh_token", string? session = "s1") => new()
    {
        Key = key,
        Type = type,
        SubjectId = subject,
        SessionId = session,
        ClientId = client,
        Description = "laptop",
        CreationTime = Now,
        Expiration = Now.AddHours(1),
        Data = "{\"value\":1}"
    };

    [Fact]
    public async Task Grant_round_trips_and_is_replaced_on_store()
    {
        var store = new MongoPersistedGrantStore(_context);
        var grant = Grant("k1");

        await store.StoreAsync(grant);
        grant.ConsumedTime = Now.AddMinutes(5);
        await store.StoreAsync(grant);

        Assert.Equivalent(grant, await store.GetAsync("k1"), strict: true);
        Assert.Equal(1, await _context.PersistedGrants.CountDocumentsAsync(FilterDefinition<PersistedGrantDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Grants_are_filtered_and_removed()
    {
        var store = new MongoPersistedGrantStore(_context);
        await store.StoreAsync(Grant("a"));
        await store.StoreAsync(Grant("b", type: "reference_token"));
        await store.StoreAsync(Grant("c", client: "spa"));
        await store.StoreAsync(Grant("d", subject: "alice", session: "s2"));

        Assert.Equal(3, (await store.GetAllAsync(new PersistedGrantFilter { SubjectId = "bob" })).Count());
        Assert.Equal(["a"], (await store.GetAllAsync(new PersistedGrantFilter { SubjectId = "bob", ClientId = "web", Type = "refresh_token" })).Select(g => g.Key));
        Assert.Equal(["d"], (await store.GetAllAsync(new PersistedGrantFilter { SessionId = "s2" })).Select(g => g.Key));

        await store.RemoveAllAsync(new PersistedGrantFilter { SubjectId = "bob", ClientId = "web" });
        await store.RemoveAsync("d");

        Assert.Null(await store.GetAsync("a"));
        Assert.Null(await store.GetAsync("b"));
        Assert.NotNull(await store.GetAsync("c"));
        Assert.Null(await store.GetAsync("d"));
    }

    [Fact]
    public async Task Grant_filter_needs_a_value()
    {
        var store = new MongoPersistedGrantStore(_context);

        await Assert.ThrowsAsync<ArgumentException>(() => store.GetAllAsync(new PersistedGrantFilter()));
        await Assert.ThrowsAsync<ArgumentException>(() => store.RemoveAllAsync(new PersistedGrantFilter()));
    }

    [Fact]
    public async Task Device_codes_round_trip_with_their_subject()
    {
        var store = new MongoDeviceFlowStore(_context, new PersistentGrantSerializer());
        var code = new DeviceCode
        {
            ClientId = "tv",
            CreationTime = Now,
            Lifetime = 300,
            RequestedScopes = ["openid", "orders.read"],
            IsOpenId = true
        };

        await store.StoreDeviceAuthorizationAsync("device-1", "123456", code);
        Assert.Equal("tv", (await store.FindByUserCodeAsync("123456"))?.ClientId);
        Assert.False((await store.FindByDeviceCodeAsync("device-1"))!.IsAuthorized);

        code.IsAuthorized = true;
        code.Subject = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "bob"), new Claim("name", "Bob")], "pwd"));
        code.AuthorizedScopes = ["openid"];
        code.SessionId = "s1";
        await store.UpdateByUserCodeAsync("123456", code);

        var authorized = await store.FindByDeviceCodeAsync("device-1");
        Assert.True(authorized!.IsAuthorized);
        Assert.Equal("bob", authorized.Subject!.FindFirst("sub")!.Value);
        var document = await _context.DeviceCodes.Find(d => d.Id == "device-1").FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal("bob", document.SubjectId);
        Assert.Equal(Now.AddSeconds(300), document.Expiration);

        await store.RemoveByDeviceCodeAsync("device-1");
        Assert.Null(await store.FindByUserCodeAsync("123456"));
    }

    [Fact]
    public async Task Signing_keys_are_stored_loaded_and_deleted()
    {
        var store = new MongoSigningKeyStore(_context);
        var key = new SerializedKey { Id = "k1", Version = 1, Created = Now, Algorithm = "RS256", Data = "protected", DataProtected = true };

        await store.StoreKeyAsync(key);
        Assert.Equivalent(key, Assert.Single(await store.LoadKeysAsync()), strict: true);

        await store.DeleteKeyAsync("k1");
        Assert.Empty(await store.LoadKeysAsync());
    }

    [Fact]
    public async Task Expiring_collections_have_ttl_indexes()
    {
        await _context.CreateIndexesAsync(TestContext.Current.CancellationToken);

        foreach (var collection in new[] { "persistedGrants", "deviceCodes" })
        {
            var indexes = await (await _context.Database.GetCollection<BsonDocument>(collection).Indexes
                .ListAsync(TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
            var ttl = Assert.Single(indexes, i => i["name"] == "Expiration_ttl");
            Assert.Equal(0, ttl["expireAfterSeconds"].ToInt32());
        }
    }
}
