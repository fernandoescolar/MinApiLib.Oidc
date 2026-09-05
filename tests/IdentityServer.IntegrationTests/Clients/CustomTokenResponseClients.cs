using IdentityServer.IntegrationTests.Clients.Setup;

namespace IdentityServer.IntegrationTests.Clients;

public class CustomTokenResponseClients : IDisposable
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

    public CustomTokenResponseClients()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseStartup<StartupWithCustomTokenResponses>())
            .Start();
        var server = _host.GetTestServer();

        _client = server.CreateClient();
    }

    [Fact]
    public async Task Extension_grant_success_should_return_custom_response()
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
                    { "outcome", "succeed"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        var fields = GetFields(response);
        Assert.Equal("some_string", fields["string_value"]);
        Assert.Equal(42, ((Int64)fields["int_value"]));

        Assert.False(fields.TryGetValue("identity_token", out _));
        Assert.False(fields.TryGetValue("refresh_token", out _));
        Assert.False(fields.TryGetValue("error", out _));
        Assert.False(fields.TryGetValue("error_description", out _));
        Assert.True(fields.TryGetValue("token_type", out _));
        Assert.True(fields.TryGetValue("expires_in", out _));

        var responseObject = fields["dto"] as JsonObject;
        Assert.NotNull(responseObject);

        var responseDto = GetDto(responseObject);
        var dto = CustomResponseDto.Create;

        Assert.Equal(dto.string_value, responseDto.string_value);
        Assert.Equal(dto.int_value, responseDto.int_value);
        Assert.Equal(dto.nested.string_value, responseDto.nested.string_value);
        Assert.Equal(dto.nested.int_value, responseDto.nested.int_value);

        Assert.False(response.IsError);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);

        var payload = GetPayload(response);
        Assert.Equal(12, payload.Count);
        Assert.Equal("https://idsvr4", payload["iss"].ToString());
        Assert.Equal("client.custom", payload["client_id"].ToString());
        Assert.Equal("bob", payload["sub"].ToString());
        Assert.Equal("local", payload["idp"].ToString());

        Assert.Equal("api", payload["aud"].ToString());

        var scopes = payload["scope"] as JsonArray;
        Assert.Equal("api1", scopes.First().ToString());

        var amr = payload["amr"] as JsonArray;
        Assert.Single(amr);
        Assert.Equal("custom", amr.First().ToString());

    }

    [Fact]
    public async Task Extension_grant_failure_should_return_custom_error_response()
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
                    { "outcome", "fail"}
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        var fields = GetFields(response);
        Assert.Equal("some_string", fields["string_value"]);
        Assert.Equal(42, ((Int64)fields["int_value"]));

        Assert.False(fields.TryGetValue("identity_token", out _));
        Assert.False(fields.TryGetValue("refresh_token", out _));
        Assert.True(fields.TryGetValue("error", out _));
        Assert.True(fields.TryGetValue("error_description", out _));
        Assert.False(fields.TryGetValue("token_type", out _));
        Assert.False(fields.TryGetValue("expires_in", out _));

        var responseObject = fields["dto"] as JsonObject;
        Assert.NotNull(responseObject);

        var responseDto = GetDto(responseObject);
        var dto = CustomResponseDto.Create;

        Assert.Equal(dto.string_value, responseDto.string_value);
        Assert.Equal(dto.int_value, responseDto.int_value);
        Assert.Equal(dto.nested.string_value, responseDto.nested.string_value);
        Assert.Equal(dto.nested.int_value, responseDto.nested.int_value);

        Assert.True(response.IsError);
        Assert.Equal("invalid_grant", response.Error);
        Assert.Equal("invalid_credential", response.ErrorDescription);
        Assert.Equal(0, response.ExpiresIn);
        Assert.Null(response.TokenType);
        Assert.Null(response.IdentityToken);
        Assert.Null(response.RefreshToken);
    }

    private CustomResponseDto GetDto(JsonObject responseObject)
    {
        return responseObject.Deserialize<CustomResponseDto>();
    }

    private Dictionary<string, object> GetFields(TokenResponse response)
    {
        Assert.NotNull(response.Json);
#if NET6_0
            var jObject = JsonObject.Create(response.Json);
#else
        var jObject = JsonObject.Create(response.Json.Value);
#endif
        return jObject!.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => (object)GetValue(kvp.Value));
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

    private JsonObject GetPayload(TokenResponse response)
    {
        var token = response.AccessToken.Split('.').Skip(1).Take(1).First();
        var json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        return JsonNode.Parse(json).AsObject();
    }
}
