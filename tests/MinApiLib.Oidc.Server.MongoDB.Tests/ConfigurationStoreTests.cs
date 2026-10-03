namespace MinApiLib.Oidc.Server.MongoDB.Tests;

public class ConfigurationStoreTests(MongoFixture mongo)
{
    private readonly OidcServerMongoContext _context = mongo.CreateContext();

    private static Client FullClient() => new()
    {
        ClientId = "web",
        ClientName = "Web",
        ClientSecrets = { new Secret("secret".Sha256(), "main", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)) },
        AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
        RequirePkce = true,
        RequireDPoP = true,
        RedirectUris = { "https://web/signin-oidc" },
        PostLogoutRedirectUris = { "https://web/signout-callback-oidc" },
        AllowedScopes = { "openid", "profile", "orders.read" },
        AllowedCorsOrigins = { "https://Web.Example.com" },
        AllowOfflineAccess = true,
        AccessTokenType = AccessTokenType.Reference,
        RefreshTokenUsage = TokenUsage.ReUse,
        RefreshTokenExpiration = TokenExpiration.Sliding,
        ConsentLifetime = 3600,
        Claims = { new ClientClaim("tenant", "contoso") },
        Properties = { ["tier"] = "gold" }
    };

    [Fact]
    public async Task Client_round_trips_every_setting()
    {
        var client = FullClient();
        await _context.UpsertAsync(client, TestContext.Current.CancellationToken);

        var loaded = await new MongoClientStore(_context).FindClientByIdAsync("web");

        Assert.NotNull(loaded);
        Assert.Equivalent(client, loaded, strict: true);
    }

    [Fact]
    public async Task Client_id_is_the_document_id_and_enums_are_strings()
    {
        await _context.UpsertAsync(FullClient(), TestContext.Current.CancellationToken);

        var document = await _context.Database.GetCollection<BsonDocument>("clients")
            .Find(Builders<BsonDocument>.Filter.Empty).FirstAsync(TestContext.Current.CancellationToken);

        Assert.Equal("web", document["_id"].AsString);
        Assert.Equal("Reference", document["AccessTokenType"].AsString);
    }

    [Fact]
    public async Task Unknown_client_is_null()
    {
        Assert.Null(await new MongoClientStore(_context).FindClientByIdAsync("missing"));
    }

    [Fact]
    public async Task Resources_are_found_by_scope_and_name()
    {
        var ct = TestContext.Current.CancellationToken;
        await _context.UpsertAsync(new IdentityResources.OpenId(), ct);
        await _context.UpsertAsync(new IdentityResources.Profile(), ct);
        await _context.UpsertAsync(new ApiScope("orders.read"), ct);
        await _context.UpsertAsync(new ApiScope("orders.write"), ct);
        await _context.UpsertAsync(new ApiResource("orders") { Scopes = { "orders.read", "orders.write" }, ApiSecrets = { new Secret("api".Sha256()) } }, ct);
        await _context.UpsertAsync(new ApiResource("invoices") { Scopes = { "invoices.read" } }, ct);
        var store = new MongoResourceStore(_context);

        var identity = await store.FindIdentityResourcesByScopeNameAsync(["openid", "orders.read"]);
        var scopes = await store.FindApiScopesByNameAsync(["orders.read", "unknown"]);
        var byScope = await store.FindApiResourcesByScopeNameAsync(["orders.write"]);
        var byName = await store.FindApiResourcesByNameAsync(["invoices"]);
        var all = await store.GetAllResourcesAsync();

        Assert.Equal("openid", Assert.Single(identity).Name);
        Assert.Equal("orders.read", Assert.Single(scopes).Name);
        var orders = Assert.Single(byScope);
        Assert.Equal("orders", orders.Name);
        Assert.Single(orders.ApiSecrets);
        Assert.Equal("invoices", Assert.Single(byName).Name);
        Assert.Equal(2, all.IdentityResources.Count);
        Assert.Equal(2, all.ApiResources.Count);
        Assert.Equal(2, all.ApiScopes.Count);
    }

    [Fact]
    public async Task Identity_resource_settings_round_trip()
    {
        var profile = new IdentityResources.Profile { Required = true, Emphasize = true, Properties = { ["a"] = "b" } };
        await _context.UpsertAsync(profile, TestContext.Current.CancellationToken);

        var loaded = Assert.Single(await new MongoResourceStore(_context).FindIdentityResourcesByScopeNameAsync(["profile"]));

        Assert.Equivalent(profile, loaded);
    }

    [Theory]
    [InlineData("https://Web.Example.com", true)]
    [InlineData("https://web.example.com", true)]
    [InlineData("https://evil.example.com", false)]
    [InlineData("", false)]
    public async Task Cors_origins_come_from_the_clients(string origin, bool allowed)
    {
        await _context.UpsertAsync(FullClient(), TestContext.Current.CancellationToken);
        await _context.CreateIndexesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(allowed, await new MongoCorsPolicyService(_context).IsOriginAllowedAsync(origin));
    }
}
