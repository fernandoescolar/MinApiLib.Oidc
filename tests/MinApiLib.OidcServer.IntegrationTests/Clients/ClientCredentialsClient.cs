using MinApiLib.OidcServer.IntegrationTests.Clients.Setup;

namespace MinApiLib.OidcServer.IntegrationTests.Clients;

public class ClientCredentialsClient : IDisposable
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

    public ClientCredentialsClient()
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
    public async Task Invalid_endpoint_should_return_404()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint + "invalid",
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Http, response.ErrorType);
        Assert.Equal("Not Found", response.Error);
        Assert.Equal(HttpStatusCode.NotFound, response.HttpStatusCode);
    }

    [Fact]
    public async Task Valid_request_single_audience_should_return_expected_payload()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"]);
        Assert.Equal("client", payload["client_id"]);
        Assert.Contains("jti", payload.Keys);
        Assert.Contains("iat", payload.Keys);

        Assert.Equal("api", payload["aud"]);

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());
    }

    [Fact]
    public async Task Valid_request_multiple_audiences_should_return_expected_payload()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1 other_api"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"]);
        Assert.Equal("client", payload["client_id"]);
        Assert.Contains("jti", payload.Keys);
        Assert.Contains("iat", payload.Keys);

        var audiences = ((JsonArray)payload["aud"]).Select(x => x.ToString());
        Assert.Equal(2, audiences.Count());
        Assert.Contains("api", audiences);
        Assert.Contains("other_api", audiences);

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());
    }

    [Fact]
    public async Task Valid_request_with_confirmation_should_return_expected_payload()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.cnf",
            ClientSecret = "foo",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(9, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.cnf", payload["client_id"].ToString());
        Assert.Contains("jti", payload.Keys);
        Assert.Contains("iat", payload.Keys);

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var cnf = payload["cnf"] as JsonObject;
        Assert.Equal("foo", cnf["x5t#S256"].ToString());
    }

    [Fact]
    public async Task Requesting_multiple_scopes_should_return_expected_payload()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1 api2"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"]);
        Assert.Equal("client", payload["client_id"]);
        Assert.Contains("jti", payload.Keys);
        Assert.Contains("iat", payload.Keys);

        Assert.Equal("api", payload["aud"]);

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal(2, scopes.Count());
        Assert.Equal("api1", scopes.First().ToString());
        Assert.Equal("api2", scopes.Skip(1).First().ToString());
    }

    [Fact]
    public async Task Request_with_no_explicit_scopes_should_return_expected_payload()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"]);
        Assert.Equal("client", payload["client_id"]);
        Assert.Contains("jti", payload.Keys);
        Assert.Contains("iat", payload.Keys);

        var audiences = ((JsonArray)payload["aud"]).Select(x => x.ToString());
        Assert.Equal(2, audiences.Count());
        Assert.Contains("api", audiences);
        Assert.Contains("other_api", audiences);

        var scopes = ((JsonArray)payload["scope"]).Select(x => x.ToString());
        Assert.Equal(3, scopes.Count());
        Assert.Contains("api1", scopes);
        Assert.Contains("api2", scopes);
        Assert.Contains("other_api", scopes);
    }

    [Fact]
    public async Task Client_without_default_scopes_skipping_scope_parameter_should_return_error()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.no_default_scopes",
            ClientSecret = "secret"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(0, response.ExpiresIn);
        Assert.Null(response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, response.Error);
    }

    [Fact]
    public async Task Request_posting_client_secret_in_body_should_succeed()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1",

            ClientCredentialStyle = ClientCredentialStyle.PostBody
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client", payload["client_id"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());
    }

    [Fact]
    public async Task Request_For_client_credentials_client_with_no_secret_should_fail()
    {
        // client_credentials without client authentication is rejected by client configuration validation.
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.no_secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, response.Error);
    }

    [Fact]
    public async Task Request_with_invalid_client_secret_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "invalid",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal("invalid_client", response.Error);
    }

    [Fact]
    public async Task Unknown_client_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "invalid",
            ClientSecret = "secret",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_client", response.Error);
    }

    [Fact]
    public async Task Code_only_client_should_not_use_client_credential_grant()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "code_only",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("unauthorized_client", response.Error);
    }

    [Fact]
    public async Task Code_and_client_creds_client_should_not_use_client_credential_grant_without_secret()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "code_and_client_creds",
            ClientSecret = "invalid",
            Scope = "api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_client", response.Error);
    }

    [Fact]
    public async Task Requesting_unknown_scope_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "unknown"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_scope", response.Error);
    }

    [Fact]
    public async Task Client_explicitly_requesting_identity_scope_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client.identityscopes",
            ClientSecret = "secret",
            Scope = "openid api1"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_scope", response.Error);
    }

    [Fact]
    public async Task Client_explicitly_requesting_offline_access_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1 offline_access"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_scope", response.Error);
    }

    [Fact]
    public async Task Requesting_unauthorized_scope_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api3"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_scope", response.Error);
    }

    [Fact]
    public async Task Requesting_authorized_and_unauthorized_scopes_should_fail()
    {
        var response = await _client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = "client",
            ClientSecret = "secret",
            Scope = "api1 api3"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("invalid_scope", response.Error);
    }

    private Dictionary<string, object> GetPayload(TokenResponse response)
    {
        var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
        var jsonString = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        var node = JsonObject.Parse(jsonString);
        return node.AsObject().AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => GetValue(kvp.Value));
    }

    public static object GetValue(JsonNode jsonNode)
    {
        try
        {
            JsonValue jsonValue = jsonNode.AsValue();
            if (jsonValue.TryGetValue(out long longValue)) return longValue;
            if (jsonValue.TryGetValue(out bool boolValue)) return boolValue;
            if (jsonValue.TryGetValue(out string stringValue)) return stringValue;
        }
        catch (InvalidOperationException) { }

        try
        {
            return jsonNode.AsObject();
        }
        catch (InvalidOperationException) { }

        try
        {
            return jsonNode.AsArray();
        }
        catch (InvalidOperationException) { }

        return null;
    }

}
