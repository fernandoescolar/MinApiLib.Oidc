using MinApiLib.OidcServer.Core.Cors;
using MinApiLib.OidcServer.Core.DPoP;
using MinApiLib.OidcServer.Core.Hosting;
using MinApiLib.OidcServer.Features.Session;
using Microsoft.AspNetCore.Routing;

namespace MinApiLib.OidcServer.IntegrationTests.Hosting;

public class EndpointRoutingTests : IDisposable
{
    private const string Category = "Endpoint routing";

    private readonly OidcServerPipeline _pipeline = new();

    public EndpointRoutingTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequireConsent = false,
            RequirePkce = false,
            AllowedScopes = { "openid" },
            RedirectUris = { "https://client1/callback" }
        });
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestMarker;

    private IEnumerable<Microsoft.AspNetCore.Http.Endpoint> MappedEndpoints()
        => _pipeline.Server.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(e => e.Metadata.GetMetadata<OidcServerEndpointMetadata>() != null);

    [Fact]
    [Trait("Category", Category)]
    public async Task Protocol_endpoints_are_mapped_and_served()
    {
        _pipeline.Initialize();

        var response = await _pipeline.BackChannelClient.GetAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = MappedEndpoints().Select(e => e.Metadata.GetMetadata<OidcServerEndpointMetadata>().Name).ToList();
        Assert.Contains(OidcServerConstants.EndpointNames.Discovery, names);
        Assert.Contains(OidcServerConstants.EndpointNames.Token, names);
        Assert.Contains(OidcServerConstants.EndpointNames.Authorize, names);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Authorization_code_flow_works()
    {
        _pipeline.Initialize();
        await _pipeline.LoginAsync("bob");

        var url = _pipeline.CreateAuthorizeUrl(clientId: "client1", responseType: "code", scope: "openid", redirectUri: "https://client1/callback", state: "state", nonce: "nonce");
        var response = await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        var code = new AuthorizeResponse(response.RequestMessage.RequestUri.ToString()).Code;

        var token = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",
            Code = code,
            RedirectUri = "https://client1/callback"
        }, TestContext.Current.CancellationToken);

        Assert.False(token.IsError);
        Assert.NotNull(token.IdentityToken);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Disabled_endpoint_returns_404()
    {
        _pipeline.Initialize();
        _pipeline.Options.Endpoints.EnableDiscoveryEndpoint = false;

        var response = await _pipeline.BackChannelClient.GetAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Disabled_endpoint_returns_404_without_running_it()
    {
        _pipeline.Initialize();
        _pipeline.Options.Endpoints.EnableTokenEndpoint = false;

        // The token endpoint has no check of its own: the base endpoint record answers 404.
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        var response = await _pipeline.BackChannelClient.PostAsync(OidcServerPipeline.TokenEndpoint, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Mtls_alias_returns_404_when_mutual_tls_is_disabled()
    {
        _pipeline.Initialize();

        var response = await _pipeline.BackChannelClient.PostAsync("https://server/connect/mtls/token", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public void ForEndpoint_conventions_apply_only_to_that_endpoint_and_its_mtls_alias()
    {
        _pipeline.ConfigureEndpoints = builder => builder.ForEndpoint(OidcServerConstants.EndpointNames.Token).WithMetadata(new TestMarker());
        _pipeline.Initialize();

        var marked = MappedEndpoints().Where(e => e.Metadata.GetMetadata<TestMarker>() != null).ToList();

        Assert.Equal(2, marked.Count());
        Assert.All(marked, e => Assert.True(e.Metadata.GetMetadata<OidcServerEndpointMetadata>().Name == OidcServerConstants.EndpointNames.Token));
        Assert.Single(marked, e => e.Metadata.GetMetadata<OidcServerEndpointMetadata>().IsMutualTlsAlias);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Unknown_endpoint_name_should_throw()
    {
        _pipeline.ConfigureEndpoints = builder => builder.ForEndpoint("Unknown");

        var act = () => _pipeline.Initialize();

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_MapOidcServer_fails_with_a_clear_error()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services => services.AddOidcServer()
                    .AddInMemoryClients([])
                    .AddInMemoryIdentityResources([])
                    .AddInMemoryApiResources([])
                    .AddInMemoryApiScopes([])
                    .AddInMemoryPersistedGrants());
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseOidcServer();
                });
            })
            .Start();
        using var client = host.GetTestClient();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken));

        Assert.Contains("MapOidcServer", ex.Message, StringComparison.Ordinal);
    }
}
