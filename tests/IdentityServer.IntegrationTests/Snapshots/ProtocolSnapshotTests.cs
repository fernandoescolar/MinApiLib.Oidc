namespace IdentityServer.IntegrationTests.Snapshots;

/// <summary>
/// Snapshot tests of the public protocol surface: a change to these documents must be deliberate.
/// Accept an intended change by replacing the .verified.json file with the .received.json one.
/// </summary>
public class ProtocolSnapshotTests : IDisposable
{
    private readonly IdentityServerPipeline _pipeline = new();

    public ProtocolSnapshotTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api1" },
            Claims = { new ClientClaim("tenant", "contoso") }
        });
        _pipeline.IdentityScopes.AddRange([new IdentityResources.OpenId(), new IdentityResources.Profile()]);
        _pipeline.ApiScopes.Add(new ApiScope("api1"));
        _pipeline.ApiResources.Add(new ApiResource("https://api1.example") { Scopes = { "api1" } });
        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Discovery_document()
    {
        var json = await _pipeline.BackChannelClient.GetStringAsync(IdentityServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken);

        Snapshot.Match(JsonNode.Parse(json));
    }

    [Fact]
    public async Task Client_credentials_access_token()
    {
        var response = await _pipeline.BackChannelClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1"
        }, TestContext.Current.CancellationToken);
        Assert.False(response.IsError);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        var header = jwt.Header.Where(h => h.Key != "kid").ToDictionary(h => h.Key, h => h.Value?.ToString());
        var payload = JsonNode.Parse(jwt.Payload.SerializeToJson());

        var document = new JsonObject
        {
            ["response"] = new JsonObject { ["token_type"] = response.TokenType, ["expires_in"] = response.ExpiresIn, ["scope"] = response.Scope },
            ["header"] = JsonSerializer.SerializeToNode(header),
            ["payload"] = payload
        };

        Snapshot.Match(document, ["iat", "nbf", "exp", "jti"]);
    }
}
