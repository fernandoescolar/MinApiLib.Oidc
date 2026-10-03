namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.DeprecatedFlows;

/// <summary>
/// The implicit flow, access tokens from the authorize endpoint and the password grant are deprecated by OAuth 2.1 and
/// RFC 9700. They stay available behind client flags that default to off.
/// </summary>
public class DeprecatedFlowTests : IDisposable
{
    private const string Category = "Deprecated flows";

    private readonly OidcServerPipeline _pipeline = new();

    public DeprecatedFlowTests()
    {
        _pipeline.Clients.AddRange(
        [
            new Client
            {
                ClientId = "implicit",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowImplicitFlow = true,
                AllowAccessTokensViaBrowser = true,
                RequireConsent = false,
                AllowedScopes = { "openid", "profile", "api1" },
                RedirectUris = { "https://implicit/callback" }
            },
            new Client
            {
                ClientId = "implicit.id_token_only",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowImplicitFlow = true,
                RequireConsent = false,
                AllowedScopes = { "openid", "profile", "api1" },
                RedirectUris = { "https://implicit/callback" }
            },
            new Client
            {
                ClientId = "implicit.not_enabled",
                AllowedGrantTypes = GrantTypes.Implicit,
                RequireConsent = false,
                AllowedScopes = { "openid" },
                RedirectUris = { "https://implicit/callback" }
            },
            new Client
            {
                ClientId = "hybrid",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Hybrid,
                AllowAccessTokensViaBrowser = true,
                RequirePkce = false,
                RequireConsent = false,
                AllowedScopes = { "openid", "profile", "api1" },
                RedirectUris = { "https://hybrid/callback" }
            },
            new Client
            {
                ClientId = "ropc",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowResourceOwnerPasswordFlow = true,
                AllowedScopes = { "openid", "api1" }
            },
            new Client
            {
                ClientId = "ropc.not_enabled",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowedScopes = { "api1" }
            }
        ]);
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.IdentityScopes.Add(new IdentityResources.Profile());
        _pipeline.ApiScopes.Add(new ApiScope("api1"));
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob", Password = "password" });

        _pipeline.Initialize();
        _pipeline.BrowserClient.AllowAutoRedirect = false;
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<ProtocolClient.AuthorizeResponse> AuthorizeAsync(string clientId, string responseType, string scope, string redirectUri = "https://implicit/callback")
    {
        await _pipeline.LoginAsync("bob");
        var url = _pipeline.CreateAuthorizeUrl(
            clientId: clientId,
            responseType: responseType,
            scope: scope,
            redirectUri: redirectUri,
            state: "state",
            nonce: "nonce");

        var response = await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(redirectUri + "#", response.Headers.Location!.ToString(), StringComparison.Ordinal);

        return new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
    }

    /// <summary>
    /// Sends an authorize request that must fail and follows the redirect to the error page.
    /// </summary>
    private async Task AuthorizeExpectingErrorAsync(string url)
    {
        var response = await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.StartsWith(OidcServerPipeline.ErrorPage, response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        await _pipeline.BrowserClient.GetAsync(response.Headers.Location, TestContext.Current.CancellationToken);
        Assert.True(_pipeline.ErrorWasCalled);
    }

    private static JwtSecurityToken Read(string jwt) => new JwtSecurityTokenHandler().ReadJwtToken(jwt);

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_id_token_returns_an_identity_token_in_the_fragment()
    {
        var response = await AuthorizeAsync("implicit.id_token_only", "id_token", "openid profile");

        Assert.Null(response.Code);
        Assert.Null(response.AccessToken);
        var idToken = Read(response.IdentityToken);
        Assert.Equal("bob", idToken.Subject);
        Assert.Contains(idToken.Claims, c => c.Type == "nonce" && c.Value == "nonce");
        Assert.Equal("state", response.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_id_token_token_returns_both_tokens_with_at_hash()
    {
        var response = await AuthorizeAsync("implicit", "id_token token", "openid profile api1");

        Assert.NotNull(response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.True(response.ExpiresIn > 0);
        Assert.Contains(Read(response.IdentityToken).Claims, c => c.Type == "at_hash");
        Assert.Equal("bob", Read(response.AccessToken).Subject);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_token_returns_only_an_access_token()
    {
        var response = await AuthorizeAsync("implicit", "token", "api1");

        Assert.Null(response.IdentityToken);
        Assert.Contains(Read(response.AccessToken).Claims, c => c.Type == "scope" && c.Value == "api1");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_id_token_without_nonce_is_rejected()
    {
        await _pipeline.LoginAsync("bob");
        var url = _pipeline.CreateAuthorizeUrl(clientId: "implicit", responseType: "id_token", scope: "openid", redirectUri: "https://implicit/callback");

        await AuthorizeExpectingErrorAsync(url);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Access_token_from_the_browser_needs_AllowAccessTokensViaBrowser()
    {
        await _pipeline.LoginAsync("bob");
        var url = _pipeline.CreateAuthorizeUrl(clientId: "implicit.id_token_only", responseType: "id_token token", scope: "openid api1",
            redirectUri: "https://implicit/callback", nonce: "nonce");

        await AuthorizeExpectingErrorAsync(url);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _pipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_client_without_AllowImplicitFlow_is_rejected()
    {
        await _pipeline.LoginAsync("bob");
        var url = _pipeline.CreateAuthorizeUrl(clientId: "implicit.not_enabled", responseType: "id_token", scope: "openid",
            redirectUri: "https://implicit/callback", nonce: "nonce");

        // The client configuration validator rejects the client, so it is unknown to the authorize endpoint.
        await AuthorizeExpectingErrorAsync(url);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Hybrid_code_token_returns_a_code_and_an_access_token()
    {
        var response = await AuthorizeAsync("hybrid", "code token", "openid api1", "https://hybrid/callback");

        Assert.NotNull(response.Code);
        Assert.NotNull(response.AccessToken);
        Assert.Null(response.IdentityToken);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Hybrid_code_id_token_token_hashes_code_and_access_token()
    {
        var response = await AuthorizeAsync("hybrid", "code id_token token", "openid api1", "https://hybrid/callback");

        var claims = Read(response.IdentityToken).Claims.ToList();
        Assert.Contains(claims, c => c.Type == "c_hash");
        Assert.Contains(claims, c => c.Type == "at_hash");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Discovery_does_not_publish_the_deprecated_flows()
    {
        var json = JsonNode.Parse(await _pipeline.BackChannelClient.GetStringAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken))!;

        var responseTypes = json["response_types_supported"]!.AsArray().Select(v => v!.GetValue<string>()).ToList();
        Assert.Equal(["code", "code id_token"], responseTypes);
        var grantTypes = json["grant_types_supported"]!.AsArray().Select(v => v!.GetValue<string>()).ToList();
        Assert.DoesNotContain("implicit", grantTypes);
        Assert.DoesNotContain("password", grantTypes);
    }

    private Task<TokenResponse> RequestPasswordTokenAsync(string clientId, string password, string scope = "api1")
        => _pipeline.BackChannelClient.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = clientId,
            ClientSecret = "secret",
            UserName = "bob",
            Password = password,
            Scope = scope
        }, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Category", Category)]
    public async Task Password_grant_issues_an_access_token_for_the_user()
    {
        var response = await RequestPasswordTokenAsync("ropc", "password");

        Assert.False(response.IsError, response.Error);
        var accessToken = Read(response.AccessToken);
        Assert.Equal("bob", accessToken.Subject);
        Assert.Contains(accessToken.Claims, c => c.Type == "amr" && c.Value == "pwd");
        Assert.Null(response.IdentityToken);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Password_grant_with_a_wrong_password_fails()
    {
        var response = await RequestPasswordTokenAsync("ropc", "wrong");

        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, response.Error);
        Assert.Equal("invalid_username_or_password", response.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Password_grant_without_AllowResourceOwnerPasswordFlow_fails()
    {
        var response = await RequestPasswordTokenAsync("ropc.not_enabled", "password");

        // The client configuration validator rejects the client.
        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Password_is_scrubbed_from_the_logs_by_default()
    {
        Assert.Contains(OidcConstants.TokenRequest.Password, _pipeline.Options.Logging.TokenRequestSensitiveValuesFilter);
    }
}
