using MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients;

public class CustomTokenRequestValidatorClient : IDisposable
{
    private const string TokenEndpoint = "https://server/connect/token";

    private readonly HttpClient _client;
    private readonly IHost _host;

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public CustomTokenRequestValidatorClient()
    {
        // Registered on this host only: a static shared with other test classes leaked into parallel tests.
        var val = new TestCustomTokenRequestValidator();

        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<Startup>()
                .ConfigureTestServices(services => services.AddTransient<ICustomTokenRequestValidator>(_ => val)))
            .Start();
        var server = _host.GetTestServer();

        _client = server.CreateClient();
    }

    [Fact]
    public async Task Client_credentials_request_should_contain_custom_response()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,

            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var fields = GetFields(response);
        Assert.Equal("custom", fields["custom"]);
    }

    [Fact]
    public async Task Extension_grant_request_should_contain_custom_response()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },
                    { "custom_credential", "custom credential"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        var fields = GetFields(response);
        Assert.Equal("custom", fields["custom"]);
    }

    private Dictionary<string, object> GetFields(TokenResponse response)
    {
        Assert.NotNull(response.Json);
#if NET6_0
        var value = response.Json;
#else
        var value = response.Json.Value;
#endif
        var jObject = JsonObject.Create(value);
        return jObject!.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.ToString());
    }
}
