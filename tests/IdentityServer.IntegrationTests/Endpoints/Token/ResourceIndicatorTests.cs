namespace IdentityServer.IntegrationTests.Endpoints.Token;

/// <summary>
/// Resource indicators (RFC 8707) at the authorize and token endpoints.
/// </summary>
public class ResourceIndicatorTests : IDisposable
{
    private const string Category = "Resource indicators";

    private const string Api1 = "https://api1.example";
    private const string Api2 = "https://api2.example";
    private const string Api3 = "https://api3.example";
    private const string RedirectUri = "https://code_client/callback";

    private readonly IdentityServerPipeline _pipeline = new();

    public ResourceIndicatorTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "code_client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequireConsent = false,
            RequirePkce = false,
            AllowOfflineAccess = true,
            AllowedScopes = { "openid", "scope1", "scope2", "scope3" },
            RedirectUris = { RedirectUri }
        });
        _pipeline.Clients.Add(new Client
        {
            ClientId = "cc_client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "scope1", "scope2", "scope3" }
        });

        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.ApiScopes.AddRange([new ApiScope("scope1"), new ApiScope("scope2"), new ApiScope("scope3")]);
        _pipeline.ApiResources.AddRange(
        [
            new ApiResource(Api1) { Scopes = { "scope1" } },
            new ApiResource(Api2) { Scopes = { "scope2" } },
            new ApiResource(Api3) { Scopes = { "scope3" }, RequireResourceIndicator = true }
        ]);

        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private static IEnumerable<string> Audiences(string jwt) => new JwtSecurityTokenHandler().ReadJwtToken(jwt).Audiences;

    private Task<TokenResponse> ClientCredentialsAsync(string scope, params string[] resources)
    {
        var request = new ClientCredentialsTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = "cc_client",
            ClientSecret = "secret",
            Scope = scope
        };
        foreach (var resource in resources)
        {
            request.Parameters.Add("resource", resource);
        }

        return _pipeline.BackChannelClient.RequestClientCredentialsTokenAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<AuthorizeResponse> AuthorizeAsync(params string[] resources)
    {
        await _pipeline.LoginAsync("bob");

        var extra = new Parameters();
        foreach (var resource in resources)
        {
            extra.Add("resource", resource);
        }

        return await _pipeline.RequestAuthorizationEndpointAsync("code_client", "code",
            "openid scope1 scope2 scope3 offline_access", RedirectUri, state: "state", nonce: "nonce", extra: extra);
    }

    private Task<TokenResponse> RedeemCodeAsync(string code, string resource = null)
    {
        var request = new AuthorizationCodeTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = "code_client",
            ClientSecret = "secret",
            Code = code,
            RedirectUri = RedirectUri
        };
        if (resource != null)
        {
            request.Parameters.Add("resource", resource);
        }

        return _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(request, TestContext.Current.CancellationToken);
    }

    private Task<TokenResponse> RefreshAsync(string refreshToken, string resource = null)
    {
        var request = new RefreshTokenRequest
        {
            Address = IdentityServerPipeline.TokenEndpoint,
            ClientId = "code_client",
            ClientSecret = "secret",
            RefreshToken = refreshToken
        };
        if (resource != null)
        {
            request.Parameters.Add("resource", resource);
        }

        return _pipeline.BackChannelClient.RequestRefreshTokenAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Without_resource_the_audience_has_every_api_except_those_requiring_an_indicator()
    {
        var response = await ClientCredentialsAsync("scope1 scope2 scope3");

        Assert.False(response.IsError);
        Assert.Equivalent(new[] { Api1, Api2 }, Audiences(response.AccessToken), strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Resource_limits_the_audience_to_that_api()
    {
        var response = await ClientCredentialsAsync("scope1 scope2 scope3", Api3);

        Assert.False(response.IsError);
        Assert.Equivalent(new[] { Api3 }, Audiences(response.AccessToken), strict: true);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("scope2", Api1)]
    [InlineData("scope1", "https://unknown.example")]
    [InlineData("scope1", "https://api1.example#fragment")]
    [InlineData("scope1", "relative/path")]
    public async Task Resource_that_is_not_an_api_of_the_scopes_or_not_an_absolute_uri_is_rejected(string scope, string resource)
    {
        var response = await ClientCredentialsAsync(scope, resource);

        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidTarget, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task More_than_one_resource_at_the_token_endpoint_is_rejected()
    {
        var response = await ClientCredentialsAsync("scope1 scope2", Api1, Api2);

        Assert.Equal(OidcConstants.TokenErrors.InvalidTarget, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_and_refresh_token_can_select_any_resource_requested_at_authorize()
    {
        var authorize = await AuthorizeAsync(Api1, Api3);

        var token = await RedeemCodeAsync(authorize.Code, Api3);
        Assert.False(token.IsError);
        Assert.Equivalent(new[] { Api3 }, Audiences(token.AccessToken), strict: true);

        var refreshed = await RefreshAsync(token.RefreshToken, Api1);
        Assert.False(refreshed.IsError);
        Assert.Equivalent(new[] { Api1 }, Audiences(refreshed.AccessToken), strict: true);

        // Selecting one resource must not shrink what the refresh token was authorized for.
        var again = await RefreshAsync(refreshed.RefreshToken, Api3);
        Assert.False(again.IsError);
        Assert.Equivalent(new[] { Api3 }, Audiences(again.AccessToken), strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_redemption_without_resource_uses_the_resources_requested_at_authorize()
    {
        var authorize = await AuthorizeAsync(Api1, Api3);

        var token = await RedeemCodeAsync(authorize.Code);

        Assert.False(token.IsError);
        Assert.Equivalent(new[] { Api1, Api3 }, Audiences(token.AccessToken), strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_redemption_for_a_resource_not_requested_at_authorize_is_rejected()
    {
        var authorize = await AuthorizeAsync(Api1);

        var token = await RedeemCodeAsync(authorize.Code, Api2);

        Assert.Equal(OidcConstants.TokenErrors.InvalidTarget, token.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Refresh_for_a_resource_not_authorized_is_rejected()
    {
        var authorize = await AuthorizeAsync(Api1);
        var token = await RedeemCodeAsync(authorize.Code);

        var refreshed = await RefreshAsync(token.RefreshToken, Api2);

        Assert.Equal(OidcConstants.TokenErrors.InvalidTarget, refreshed.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_without_resources_can_select_an_api_of_the_granted_scopes()
    {
        var authorize = await AuthorizeAsync();

        var token = await RedeemCodeAsync(authorize.Code, Api2);

        Assert.False(token.IsError);
        Assert.Equivalent(new[] { Api2 }, Audiences(token.AccessToken), strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_resource_at_authorize_is_rejected_with_invalid_target()
    {
        var authorize = await AuthorizeAsync("https://unknown.example");

        Assert.Null(authorize);
        Assert.True(_pipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidTarget, _pipeline.ErrorMessage.Error);
    }
}
