namespace IdentityServer.IntegrationTests.Endpoints.Revocation;

public class RevocationTests : IDisposable
{
    private const string Category = "RevocationTests endpoint";

    private string client_id = "client";
    private string client_secret = "secret";
    private string redirect_uri = "https://client/callback";

    private string scope_name = "api";
    private string scope_secret = "api_secret";

    private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public RevocationTests()
    {
        _mockPipeline.Clients.Add(new Client
        {
            ClientId = client_id,
            ClientSecrets = new List<Secret> { new Secret(client_secret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequireConsent = false,
            RequirePkce = false,
            AllowOfflineAccess = true,
            AllowedScopes = new List<string> { "api" },
            RedirectUris = new List<string> { redirect_uri },
            AccessTokenType = AccessTokenType.Reference,
            RefreshTokenUsage = TokenUsage.ReUse
        });

        _mockPipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = new Claim[]
            {
                    new Claim("name", "Bob Loblaw"),
                    new Claim("email", "bob@loblaw.com"),
                    new Claim("role", "Attorney")
            }
        });

        _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId()
            });

        _mockPipeline.ApiResources.AddRange(new ApiResource[] {
                new ApiResource
                {
                    Name = "api",
                    ApiSecrets = new List<Secret> { new Secret(scope_secret.Sha256()) },
                    Scopes = { scope_name }
                }
            });

        _mockPipeline.ApiScopes.AddRange(new ApiScope[]
        {
                new ApiScope
                {
                    Name = scope_name
                }
        });

        _mockPipeline.Initialize();
    }

    private class Tokens
    {
        public Tokens(TokenResponse response)
        {
            AccessToken = response.AccessToken;
            RefreshToken = response.RefreshToken;
        }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    private async Task<Tokens> GetTokensAsync()
    {
        await _mockPipeline.LoginAsync("bob");

        var authorizationResponse = await _mockPipeline.RequestAuthorizationEndpointAsync(
            client_id,
            "code",
            "api offline_access",
            "https://client/callback");

        Assert.False(authorizationResponse.IsError);
        Assert.NotNull(authorizationResponse.Code);

        var tokenResponse = await _mockPipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Code = authorizationResponse.Code,
            RedirectUri = redirect_uri
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResponse.IsError);
        Assert.NotNull(tokenResponse.AccessToken);
        Assert.NotNull(tokenResponse.RefreshToken);

        return new Tokens(tokenResponse);
    }

    private Task<bool> IsAccessTokenValidAsync(Tokens tokens)
    {
        return IsAccessTokenValidAsync(tokens.AccessToken);
    }

    private async Task<bool> IsAccessTokenValidAsync(string token)
    {
        var response = await _mockPipeline.BackChannelClient.IntrospectTokenAsync(new TokenIntrospectionRequest
        {
            Address = IdentityServerPipeline.IntrospectionEndpoint,
            ClientId = scope_name,
            ClientSecret = scope_secret,

            Token = token,
            TokenTypeHint = OidcConstants.TokenTypes.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        return response.IsError == false && response.IsActive;
    }

    private async Task<bool> UseRefreshTokenAsync(Tokens tokens)
    {
        var response = await _mockPipeline.BackChannelClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            RefreshToken = tokens.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        if (response.IsError)
        {
            return false;
        }

        tokens.AccessToken = response.AccessToken;
        return true;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Get_request_should_return_405()
    {
        var response = await _mockPipeline.BackChannelClient.GetAsync(IdentityServerPipeline.RevocationEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Post_without_form_urlencoded_should_return_415()
    {
        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.RevocationEndpoint, null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_valid_access_token_should_return_success()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.False((await IsAccessTokenValidAsync(tokens)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_valid_access_token_belonging_to_another_client_should_return_success_but_not_revoke_token()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = "not_owner",
            ClientSecret = client_secret,

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_valid_refresh_token_should_return_success()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await UseRefreshTokenAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);

        Assert.False((await UseRefreshTokenAsync(tokens)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_valid_refresh_token_belonging_to_another_client_should_return_success_but_not_revoke_token()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await UseRefreshTokenAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = "not_owner",
            ClientSecret = client_secret,

            Token = tokens.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);

        Assert.True((await UseRefreshTokenAsync(tokens)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_invalid_access_token_should_return_success()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);

        Assert.False((await IsAccessTokenValidAsync(tokens)));

        result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoke_invalid_refresh_token_should_return_success()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await UseRefreshTokenAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);

        Assert.False((await UseRefreshTokenAsync(tokens)));

        result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = client_secret,

            Token = tokens.RefreshToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_client_id_should_return_error()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = "not_valid",
            ClientSecret = client_secret,

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("invalid_client", result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_credentials_should_return_error()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var result = await _mockPipeline.BackChannelClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = IdentityServerPipeline.RevocationEndpoint,
            ClientId = client_id,
            ClientSecret = "not_valid",

            Token = tokens.AccessToken
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal("invalid_client", result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_token_should_return_error()
    {
        var data = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "client_secret", client_secret }
            };

        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.RevocationEndpoint, new FormUrlEncodedContent(data), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await ProtocolResponse.FromHttpResponseAsync<TokenRevocationResponse>(response, TestContext.Current.CancellationToken);
        Assert.True(result.IsError);
        Assert.Equal("invalid_request", result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_token_type_hint_should_return_error()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var data = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "token", tokens.AccessToken },
                { "token_type_hint", "not_valid" }
            };

        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.RevocationEndpoint, new FormUrlEncodedContent(data), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await ProtocolResponse.FromHttpResponseAsync<TokenRevocationResponse>(response, TestContext.Current.CancellationToken);
        Assert.True(result.IsError);
        Assert.Equal("unsupported_token_type", result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_access_token_but_missing_token_type_hint_should_succeed()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await IsAccessTokenValidAsync(tokens)));

        var data = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "token", tokens.AccessToken }
            };

        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.RevocationEndpoint, new FormUrlEncodedContent(data), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.False((await IsAccessTokenValidAsync(tokens)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_refresh_token_but_missing_token_type_hint_should_succeed()
    {
        var tokens = await GetTokensAsync();
        Assert.True((await UseRefreshTokenAsync(tokens)));

        var data = new Dictionary<string, string>
            {
                { "client_id", client_id },
                { "client_secret", client_secret },
                { "token", tokens.RefreshToken }
            };

        var response = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.RevocationEndpoint, new FormUrlEncodedContent(data), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.False((await UseRefreshTokenAsync(tokens)));
    }

}
