namespace MinApiLib.OidcServer.IntegrationTests.Endpoints.Discovery;

public class AutomaticKeyManagementTests : IDisposable
{
    private const string Category = "Automatic key management";

    private readonly OidcServerPipeline _pipeline = new();

    public AutomaticKeyManagementTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" }
        });
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.OnPostConfigureServices += services => services.AddSingleton<ISigningKeyStore, InMemorySigningKeyStore>();
        _pipeline.Initialize();
        _pipeline.Options.KeyManagement.Enabled = true;
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Tokens_are_signed_with_an_automatic_key_published_with_the_static_one()
    {
        var token = await _pipeline.BackChannelClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api"
        }, TestContext.Current.CancellationToken);
        Assert.False(token.IsError);

        var kid = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Header.Kid;
        var store = (InMemorySigningKeyStore)_pipeline.Server.Services.GetRequiredService<ISigningKeyStore>();
        Assert.Single((await store.LoadKeysAsync()), k => k.Id == kid);

        var jwks = JsonNode.Parse(await _pipeline.BackChannelClient.GetStringAsync(OidcServerPipeline.DiscoveryKeysEndpoint, TestContext.Current.CancellationToken));
        var keys = jwks["keys"].AsArray();
        Assert.Contains(kid, keys.Select(k => k["kid"].ToString()));
        Assert.Equal(2, keys.Select(k => k["kid"].ToString()).Count());
        Assert.All(keys, k => Assert.True(k["d"] == null && k["p"] == null));
    }
}
