using MinApiLib.OidcServer.IntegrationTests.Endpoints.Introspection.Setup;

namespace MinApiLib.OidcServer.IntegrationTests.Endpoints.Introspection;

public class IntrospectionTests : IDisposable
{
    private const string Category = "Introspection endpoint";
    private const string IntrospectionEndpoint = "https://server/connect/introspect";
    private const string TokenEndpoint = "https://server/connect/token";

    private readonly HttpClient _client;
    private readonly HttpMessageHandler _handler;
    private readonly IHost _host;

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public IntrospectionTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<Startup>())
            .Start();
        var server = _host.GetTestServer();

        _handler = server.CreateHandler();
        _client = server.CreateClient();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Empty_request_should_fail()
    {
        var form = new Dictionary<string, string>();

        var response = await _client.PostAsync(IntrospectionEndpoint, new FormUrlEncodedContent(form), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_scope_should_fail()
    {
        var form = new Dictionary<string, string>();

        _client.SetBasicAuthentication("unknown", "invalid");
        var response = await _client.PostAsync(IntrospectionEndpoint, new FormUrlEncodedContent(form), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_scope_secret_should_fail()
    {
        var form = new Dictionary<string, string>();

        _client.SetBasicAuthentication("api1", "invalid");
        var response = await _client.PostAsync(IntrospectionEndpoint, new FormUrlEncodedContent(form), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_token_should_fail()
    {
        var form = new Dictionary<string, string>();

        _client.SetBasicAuthentication("api1", "secret");
        var response = await _client.PostAsync(IntrospectionEndpoint, new FormUrlEncodedContent(form), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_token_should_fail()
    {
        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api1",
            ClientSecret = "secret",

            Token = "invalid"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(introspectionResponse.IsActive);
        Assert.False(introspectionResponse.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Content_type_should_fail()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var data = new
        {
            client_id = "api1",
            client_secret = "secret",
            token = tokenResponse.AccessToken
        };
        var json = JsonSerializer.Serialize(data);

        var client = new HttpClient(_handler);
        var response = await client.PostAsync(IntrospectionEndpoint, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_token_and_valid_scope_should_succeed()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api1",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(introspectionResponse.IsActive);
        Assert.False(introspectionResponse.IsError);

        var scopes = from c in introspectionResponse.Claims
                     where c.Type == "scope"
                     select c;

        Assert.Single(scopes);
        Assert.Equal("api1", scopes.First().Value);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Response_data_should_be_valid_using_single_scope()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api1",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(introspectionResponse.Json);
        var values = JsonObject.Create(introspectionResponse.Json.Value);

        Assert.Equal("String", values["aud"].GetValue<string>().GetType().Name);
        Assert.Equal("String", values["iss"].GetValue<string>().GetType().Name);
        Assert.Equal("Int64", values["nbf"].GetValue<long>().GetType().Name);
        Assert.Equal("Int64", values["exp"].GetValue<long>().GetType().Name);
        Assert.Equal("String", values["client_id"].GetValue<string>().GetType().Name);
        Assert.Equal("Boolean", values["active"].GetValue<bool>().GetType().Name);
        Assert.Equal("String", values["scope"].GetValue<string>().GetType().Name);
        Assert.Equal("api1", values["scope"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Response_data_should_be_valid_using_multiple_scopes_multiple_audiences()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",

            Scope = "api2 api3-a api3-b",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api3",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(introspectionResponse.Json);
        var values = JsonObject.Create(introspectionResponse.Json.Value);

        Assert.Equal("JsonArray", values["aud"].AsArray().GetType().Name);

        var audiences = values["aud"].AsArray();
        foreach (var aud in audiences)
        {
            Assert.Equal("String", aud.GetValue<string>().GetType().Name);
        }

        Assert.Equal("String", values["iss"].GetValue<string>().GetType().Name);
        Assert.Equal("Int64", values["nbf"].GetValue<long>().GetType().Name);
        Assert.Equal("Int64", values["exp"].GetValue<long>().GetType().Name);
        Assert.Equal("String", values["client_id"].GetValue<string>().GetType().Name);
        Assert.Equal("Boolean", values["active"].GetValue<bool>().GetType().Name);
        Assert.Equal("String", values["scope"].GetValue<string>().GetType().Name);

        var scopes = values["scope"].GetValue<string>();
        Assert.Equal("api3-a api3-b", scopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Response_data_should_be_valid_using_multiple_scopes_single_audience()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",

            Scope = "api3-a api3-b",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api3",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(introspectionResponse.Json);
        var values = JsonObject.Create(introspectionResponse.Json.Value);

        Assert.Equal("String", values["aud"].GetValue<string>().GetType().Name);
        Assert.Equal("String", values["aud"].GetValue<string>().GetType().Name);
        Assert.Equal("String", values["iss"].GetValue<string>().GetType().Name);
        Assert.Equal("Int64", values["nbf"].GetValue<long>().GetType().Name);
        Assert.Equal("Int64", values["exp"].GetValue<long>().GetType().Name);
        Assert.Equal("String", values["client_id"].GetValue<string>().GetType().Name);
        Assert.Equal("Boolean", values["active"].GetValue<bool>().GetType().Name);
        Assert.Equal("String", values["scope"].GetValue<string>().GetType().Name);
        var scopes = values["scope"].ToString();
        Assert.Equal("api3-a api3-b", scopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_with_many_scopes_but_api_should_only_see_its_own_scopes()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client3",
            ClientSecret = "secret",

            Scope = "api1 api2 api3-a",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api3",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(introspectionResponse.IsActive);
        Assert.False(introspectionResponse.IsError);

        var scopes = from c in introspectionResponse.Claims
                     where c.Type == "scope"
                     select c.Value;

        Assert.Single(scopes);
        Assert.Equal("api3-a", scopes.First());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_token_with_valid_multiple_scopes()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",

            Scope = "api1 api2",
        }, cancellationToken: TestContext.Current.CancellationToken);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api1",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(introspectionResponse.IsActive);
        Assert.False(introspectionResponse.IsError);

        var scopes = from c in introspectionResponse.Claims
                     where c.Type == "scope"
                     select c;

        Assert.Single(scopes);
        Assert.Equal("api1", scopes.First().Value);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_token_with_invalid_scopes_should_fail()
    {
        var tokenResponse = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",

            Scope = "api1",
        }, cancellationToken: TestContext.Current.CancellationToken);

        var introspectionResponse = await _client.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IntrospectionEndpoint,
            ClientId = "api2",
            ClientSecret = "secret",

            Token = tokenResponse.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(introspectionResponse.IsActive);
        Assert.False(introspectionResponse.IsError);
    }
}
