namespace IdentityServer.IntegrationTests.Endpoints.Nonce;

public class NonceTests : IDisposable
{
    private const string Category = "Nonce pattern";

    private readonly IdentityServerPipeline _mockPipeline = new();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public NonceTests()
    {
        _mockPipeline.Clients.AddRange(new Client[] {
            new()
            {
                ClientId = "client1",
                ClientSecrets = new List<Secret> {
                    new()
                    {
                        Value = "secret".Sha256()
                    }
                },
                AllowedGrantTypes = GrantTypes.CodeAndNonce,
                RequireConsent = false,
                RequirePkce = false,
                AllowedScopes = ["openid", "profile", "email"],
                RedirectUris = ["https://client1/callback"],
            },
            new() {
                ClientId = "client2",
                ClientSecrets = [
                    new()
                    {
                        Value = "secret".Sha256()
                    }
                ],
                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = false,
                RequirePkce = false,
                AllowedScopes = new List<string> { "openid", "profile", "email" },
                RedirectUris = new List<string> { "https://client2/callback" },
            }
        });

        _mockPipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = new Claim[]
            {
                new("name", "Bob Loblaw"),
                new("email", "bob@loblaw.com"),
                new("role", "Attorney")
            }
        });

        _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email()
        });
        // _mockPipeline.ApiResources.AddRange(new ApiResource[] {
        //     new ApiResource
        //     {
        //         Name = "api",
        //         Scopes = { "api1", "api2" }
        //     }
        // });
        // _mockPipeline.ApiScopes.AddRange(new ApiScope[] {
        //     new ApiScope
        //     {
        //         Name = "api1"
        //     },
        //     new ApiScope
        //     {
        //         Name = "api2"
        //     }
        // });

        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task happy_path()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid profile email",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.StartsWith("https://client1/callback", response.RequestMessage.RequestUri.ToString());
        Assert.Contains("code=", response.RequestMessage.RequestUri.Query);

        var authorizeResponse = new AuthorizeResponse(response.RequestMessage.RequestUri.ToString());
        var code = authorizeResponse.Code;

        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "redirect_uri", "https://client1/callback" },
            { "code", code },
        };
        var form = new FormUrlEncodedContent(data);
        var request = new HttpRequestMessage(HttpMethod.Post, IdentityServerPipeline.TokenEndpoint);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("client1:secret"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        request.Content = form;

        var tokenResponse = await _mockPipeline.BrowserClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var tokenResponseJson = JsonNode.Parse(tokenResponseContent).AsObject();
        Assert.NotNull(tokenResponseJson);


        var accessToken = tokenResponseJson["access_token"]?.ToString();
        Assert.False(string.IsNullOrEmpty(accessToken));

        var nonceRequest = new HttpRequestMessage(HttpMethod.Post, IdentityServerPipeline.NonceEndpoint);
        nonceRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "token", accessToken } });

        nonceRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);

        var nonceResponse = await _mockPipeline.BrowserClient.SendAsync(nonceRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, nonceResponse.StatusCode);

        var nonceResponseContent = await nonceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var nonceResponseJson = JsonNode.Parse(nonceResponseContent).AsObject();
        Assert.NotNull(nonceResponseJson);

        var nonce = nonceResponseJson["nonce"]?.ToString();
        Assert.False(string.IsNullOrEmpty(nonce));

        var secondUrl = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile email",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "urn:auth:nonce",
            loginHintToken: nonce);
        var secondResponse = await _mockPipeline.BrowserClient.GetAsync(secondUrl, TestContext.Current.CancellationToken);

        Assert.StartsWith("https://client2/callback", secondResponse.RequestMessage.RequestUri.ToString());
        Assert.Contains("code=", secondResponse.RequestMessage.RequestUri.Query);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task nonce_sign_in_issues_id_token_with_the_new_session_id()
    {
        var nonce = await GetNonceForBobAsync();

        // Simulate a different browser (e.g. a web view): no IdentityServer session yet.
        _mockPipeline.RemoveLoginCookie();
        _mockPipeline.RemoveSessionCookie();

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile email",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "urn:auth:nonce",
            loginHintToken: nonce);
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.StartsWith("https://client2/callback", response.RequestMessage.RequestUri.ToString());
        var code = new AuthorizeResponse(response.RequestMessage.RequestUri.ToString()).Code;
        Assert.False(string.IsNullOrEmpty(code));

        var tokens = await RedeemCodeAsync("client2", "https://client2/callback", code);
        var idToken = new JwtSecurityTokenHandler().ReadJwtToken(tokens["id_token"].ToString());

        var sessionCookie = _mockPipeline.GetSessionCookie();
        Assert.NotNull(sessionCookie);
        Assert.Equal(sessionCookie.Value, idToken.Claims.Single(c => c.Type == JwtClaimTypes.SessionId).Value);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task nonce_cannot_be_used_twice()
    {
        var nonce = await GetNonceForBobAsync();

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "urn:auth:nonce",
            loginHintToken: nonce);

        await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        _mockPipeline.RemoveLoginCookie();
        _mockPipeline.RemoveSessionCookie();
        await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
    }

    private async Task<string> GetNonceForBobAsync()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid profile email",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        var code = new AuthorizeResponse(response.RequestMessage.RequestUri.ToString()).Code;

        var tokens = await RedeemCodeAsync("client1", "https://client1/callback", code);
        var accessToken = tokens["access_token"].ToString();

        var nonceRequest = new HttpRequestMessage(HttpMethod.Post, IdentityServerPipeline.NonceEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "token", accessToken } })
        };
        nonceRequest.Headers.Authorization = BasicAuthentication("client1");

        var nonceResponse = await _mockPipeline.BackChannelClient.SendAsync(nonceRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, nonceResponse.StatusCode);

        var json = JsonNode.Parse(await nonceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).AsObject();
        return json["nonce"].ToString();
    }

    private async Task<JsonObject> RedeemCodeAsync(string clientId, string redirectUri, string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, IdentityServerPipeline.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "redirect_uri", redirectUri },
                { "code", code },
            })
        };
        request.Headers.Authorization = BasicAuthentication(clientId);

        var response = await _mockPipeline.BackChannelClient.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).AsObject();
    }

    private static AuthenticationHeaderValue BasicAuthentication(string clientId)
        => new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:secret")));
}
