using MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients;

public class DiscoveryClientTests : IDisposable
{
    private const string DiscoveryEndpoint = "https://server/.well-known/openid-configuration";

    private readonly HttpClient _client;
    private readonly IHost _host;

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public DiscoveryClientTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<Startup>())
            .Start();
        var server = _host.GetTestServer();

        _client = server.CreateClient();
    }

    [Fact]
    public async Task Discovery_document_should_have_expected_values()
    {
        var doc = await _client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = DiscoveryEndpoint,
            Policy =
                {
                    ValidateIssuerName = false
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("https://server/connect/token", doc.TokenEndpoint);
        Assert.Equal("https://server/connect/authorize", doc.AuthorizeEndpoint);
        Assert.Equal("https://server/connect/introspect", doc.IntrospectionEndpoint);
        Assert.Equal("https://server/connect/endsession", doc.EndSessionEndpoint);

        Assert.Single(doc.KeySet.Keys);
        Assert.NotNull(doc.KeySet.Keys.First().E);
        Assert.NotNull(doc.KeySet.Keys.First().N);
    }
}
