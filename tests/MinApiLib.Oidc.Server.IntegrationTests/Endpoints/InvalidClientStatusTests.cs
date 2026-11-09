namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints;

/// <summary>
/// invalid_client (RFC 6749 section 5.2): 401 with a Basic challenge when the client authenticated with the
/// Authorization header, 400 when its credentials were in the body. The PAR endpoint answers 401 either way.
/// </summary>
public class InvalidClientStatusTests : IDisposable
{
    private const string Category = "Client authentication";

    private readonly OidcServerPipeline _pipeline = new();

    public InvalidClientStatusTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" },
        });
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(OidcServerPipeline.TokenEndpoint, true, HttpStatusCode.Unauthorized)]
    [InlineData(OidcServerPipeline.TokenEndpoint, false, HttpStatusCode.BadRequest)]
    [InlineData(OidcServerPipeline.DeviceAuthorization, true, HttpStatusCode.Unauthorized)]
    [InlineData(OidcServerPipeline.DeviceAuthorization, false, HttpStatusCode.BadRequest)]
    [InlineData(OidcServerPipeline.NonceEndpoint, true, HttpStatusCode.Unauthorized)]
    [InlineData(OidcServerPipeline.NonceEndpoint, false, HttpStatusCode.BadRequest)]
    [InlineData(OidcServerPipeline.RevocationEndpoint, true, HttpStatusCode.Unauthorized)]
    [InlineData(OidcServerPipeline.RevocationEndpoint, false, HttpStatusCode.BadRequest)]
    [InlineData(OidcServerPipeline.BaseUrl + "/connect/par", true, HttpStatusCode.Unauthorized)]
    [InlineData(OidcServerPipeline.BaseUrl + "/connect/par", false, HttpStatusCode.Unauthorized)]
    public async Task Wrong_client_secret_gets_invalid_client(string url, bool inHeader, HttpStatusCode expected)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "token", "token" },
            { "response_type", "code" },
            { "redirect_uri", "https://client/callback" },
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (inHeader)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("client:wrong")));
        }
        else
        {
            form["client_id"] = "client";
            form["client_secret"] = "wrong";
        }
        request.Content = new FormUrlEncodedContent(form);

        var response = await _pipeline.BackChannelClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, json["error"]!.GetValue<string>());
        if (expected == HttpStatusCode.Unauthorized)
        {
            Assert.Equal("Basic realm=\"OidcServer\"", Assert.Single(response.Headers.WwwAuthenticate).ToString());
        }
        else
        {
            Assert.Empty(response.Headers.WwwAuthenticate);
        }
    }
}
