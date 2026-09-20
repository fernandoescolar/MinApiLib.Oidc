namespace MinApiLib.OidcServer.MongoDB.Tests;

/// <summary>
/// A server that keeps everything in MongoDB: the client and API come from the database, the reference token is a
/// persisted grant and the signing key is created by automatic key management in the database.
/// </summary>
public sealed class ServerEndToEndTests(MongoFixture mongo) : IAsyncDisposable
{
    private readonly string _database = "e2e_" + Guid.NewGuid().ToString("N");
    private IHost? _host;

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    private async Task<HttpClient> StartAsync()
    {
        var context = new OidcServerMongoContext(mongo.Client.GetDatabase(_database));
        var ct = TestContext.Current.CancellationToken;
        await context.UpsertAsync(new Client
        {
            ClientId = "worker",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AccessTokenType = AccessTokenType.Reference,
            AllowedScopes = { "orders.read" }
        }, ct);
        await context.UpsertAsync(new ApiScope("orders.read"), ct);
        await context.UpsertAsync(new ApiResource("orders") { Scopes = { "orders.read" }, ApiSecrets = { new Secret("api-secret".Sha256()) } }, ct);

        _host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddOidcServer(options => options.KeyManagement.Enabled = true)
                    .AddMongoStores(options =>
                    {
                        options.ConnectionString = mongo.ConnectionString;
                        options.DatabaseName = _database;
                    }))
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseOidcServer();
                    app.UseEndpoints(endpoints => endpoints.MapOidcServer());
                }))
            .StartAsync(ct);

        return _host.GetTestClient();
    }

    [Fact]
    public async Task Tokens_grants_and_keys_live_in_MongoDB()
    {
        var http = await StartAsync();
        var ct = TestContext.Current.CancellationToken;

        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "worker",
            ["client_secret"] = "secret",
            ["scope"] = "orders.read"
        });
        var tokenResponse = await http.PostAsync("https://server/connect/token", tokenRequest, ct);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var accessToken = JsonElement.Parse(await tokenResponse.Content.ReadAsStringAsync(ct)).GetProperty("access_token").GetString()!;

        using var introspection = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "orders",
            ["client_secret"] = "api-secret"
        });
        var introspectionResponse = await http.PostAsync("https://server/connect/introspect", introspection, ct);
        var result = JsonElement.Parse(await introspectionResponse.Content.ReadAsStringAsync(ct));
        Assert.True(result.GetProperty("active").GetBoolean());
        Assert.Equal("worker", result.GetProperty("client_id").GetString());

        var jwks = JsonElement.Parse(await http.GetStringAsync("https://server/.well-known/openid-configuration/jwks", ct));
        Assert.NotEmpty(jwks.GetProperty("keys").EnumerateArray());

        var context = new OidcServerMongoContext(mongo.Client.GetDatabase(_database));
        var grant = await context.PersistedGrants.Find(g => g.ClientId == "worker").SingleAsync(ct);
        Assert.Equal(OidcServerConstants.PersistedGrantTypes.ReferenceToken, grant.Type);
        Assert.NotNull(grant.Expiration);
        Assert.True(await context.SigningKeys.CountDocumentsAsync(FilterDefinition<SigningKeyDocument>.Empty, cancellationToken: ct) >= 1);
    }

    [Fact]
    public async Task Unknown_client_in_MongoDB_is_rejected()
    {
        var http = await StartAsync();

        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "nobody",
            ["client_secret"] = "secret"
        });
        var response = await http.PostAsync("https://server/connect/token", tokenRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_client", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }
}
