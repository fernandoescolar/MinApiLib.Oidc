using MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients;

public class ExtensionGrantClient : IDisposable
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

    public ExtensionGrantClient()
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
    public async Task Valid_client_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = Int64.Parse(payload["exp"].ToString());
        Assert.True(exp < unixNow + 3605);
        Assert.True(exp > unixNow + 3595);

        Assert.Equal(12, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());
        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("custom", amr.First().ToString());
    }

    [Fact]
    public async Task Valid_client_with_extra_claim_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "extra_claim", "extra_value" },
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = Int64.Parse(payload["exp"].ToString());
        Assert.True(exp < unixNow + 3605);
        Assert.True(exp > unixNow + 3595);

        Assert.Equal(13, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());
        Assert.Equal("extra_value", payload["extra_claim"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("custom", amr.First().ToString());
    }

    [Fact]
    public async Task Valid_client_with_refreshed_extra_claim_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "extra_claim", "extra_value" },
                    { "scope", "api1 offline_access" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.NotNull(response.RefreshToken);

        var refreshResponse = await _client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = TokenEndpoint,

            ClientId = "client.custom",
            ClientSecret = "secret",

            RefreshToken = response.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(refreshResponse.IsError);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.HttpStatusCode);
        Assert.Equal(3600, refreshResponse.ExpiresIn);
        Assert.Equal("Bearer", refreshResponse.TokenType);
        Assert.Null(refreshResponse.IdentityToken);
        Assert.NotNull(refreshResponse.RefreshToken);

        var payload = GetPayload(refreshResponse);

        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = Int64.Parse(payload["exp"].ToString());
        Assert.True(exp < unixNow + 3605);
        Assert.True(exp > unixNow + 3595);

        Assert.Equal(13, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());
        Assert.Equal("extra_value", payload["extra_claim"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("custom", amr.First().ToString());
    }

    [Fact]
    public async Task Valid_client_no_subject_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom.nosubject",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(8, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());
    }

    [Fact]
    public async Task Valid_client_with_default_scopes_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.NotNull(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(12, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("custom", amr.First().ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal(3, scopes.Count());
        Assert.Equal("api1", scopes.First().ToString());
        Assert.Equal("api2", scopes.Skip(1).First().ToString());
        Assert.Equal("offline_access", scopes.Skip(2).First().ToString());
    }

    [Fact]
    public async Task Valid_client_missing_grant_specific_data_should_fail()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, response.Error);
        Assert.Equal("invalid_custom_credential", response.ErrorDescription);
    }

    [Fact]
    public async Task Valid_client_using_unsupported_grant_type_should_fail()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "invalid",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("unsupported_grant_type", response.Error);
    }

    [Fact]
    public async Task Valid_client_using_unauthorized_grant_type_should_fail()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "custom2",

            ClientId = "client.custom",
            ClientSecret = "secret",

            Parameters =
                {
                    { "custom_credential", "custom credential"},
                    { "scope", "api1" }
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(response.IsError);
        Assert.Equal(ResponseErrorType.Protocol, response.ErrorType);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpStatusCode);
        Assert.Equal("unsupported_grant_type", response.Error);
    }

    [Fact()]
    public async Task Dynamic_lifetime_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "lifetime", "5000"},
                    { "sub",  "818727"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(5000, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = Int64.Parse(payload["exp"].ToString());
        Assert.True(exp < unixNow + 5005);
        Assert.True(exp > unixNow + 4995);

        Assert.Equal(12, payload.Count());
        Assert.True(payload.ContainsKey("nbf"));
        Assert.True(payload.ContainsKey("exp"));
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.dynamic", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());
        Assert.True(payload.ContainsKey("auth_time"));
        Assert.True(payload.ContainsKey("jti"));
        Assert.True(payload.ContainsKey("iat"));

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("delegation", amr.First().ToString());
    }

    [Fact]
    public async Task Dynamic_token_type_jwt_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "type", "jwt"},
                    { "sub",  "818727"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        Assert.Contains(".", response.AccessToken);
    }

    [Fact]
    public async Task Impersonate_client_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "type", "jwt"},
                    { "impersonated_client", "impersonated_client_id"},
                    { "sub",  "818727"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        Assert.Contains(".", response.AccessToken);

        var jwt = new JwtSecurityToken(response.AccessToken);
        Assert.Equal("impersonated_client_id", jwt.Payload["client_id"].ToString());
    }

    [Fact]
    public async Task Dynamic_token_type_reference_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "type", "reference"},
                    { "sub",  "818727"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        Assert.DoesNotContain(".", response.AccessToken);
    }

    [Fact]
    public async Task Dynamic_client_claims_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "claim", "extra_claim"},
                    { "sub",  "818727"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(13, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.dynamic", payload["client_id"].ToString());
        Assert.Equal("818727", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());

        Assert.Equal("extra_claim", payload["client_extra"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("delegation", amr.First().ToString());
    }

    [Fact]
    public async Task Dynamic_client_claims_no_sub_should_succeed()
    {
        var response = await _client.RequestTokenAsync(new TokenRequest
        {
            Address = TokenEndpoint,
            GrantType = "dynamic",

            ClientId = "client.dynamic",
            ClientSecret = "secret",

            Parameters =
                {
                    { "scope", "api1" },

                    { "claim", "extra_claim"},
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(response.IsError);
        Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);

        Assert.Equal(9, payload.Count());
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.dynamic", payload["client_id"].ToString());
        Assert.Equal("extra_claim", payload["client_extra"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());
    }

    private JsonObject GetPayload(TokenResponse response)
    {
        var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
        var json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        return JsonNode.Parse(json).AsObject();
    }

}
