namespace IdentityServer.IntegrationTests.Conformance.Basic;

public class ResponseTypeResponseModeTests : IDisposable
{
    private const string Category = "Conformance.Basic.ResponseTypeResponseModeTests";

    private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public ResponseTypeResponseModeTests()
    {
        _mockPipeline.Initialize();
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        _mockPipeline.Clients.Add(new Client
        {
            Enabled = true,
            ClientId = "code_client",
            ClientSecrets = new List<Secret>
                {
                    new Secret("secret".Sha512())
                },

            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = { "openid" },

            RequireConsent = false,
            RequirePkce = false,
            RedirectUris = new List<string>
                {
                    "https://code_client/callback"
                }
        });

        _mockPipeline.IdentityScopes.Add(new IdentityResources.OpenId());

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
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_with_response_type_code_supported()
    {
        await _mockPipeline.LoginAsync("bob");

        var metadata = await _mockPipeline.BackChannelClient.GetAsync(IdentityServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);

        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();

        var url = _mockPipeline.CreateAuthorizeUrl(
                       clientId: "code_client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://code_client/callback",
                       state: state,
                       nonce: nonce);
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);
        Assert.Equal(state, authorization.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_missing_response_type_rejected()
    {
        await _mockPipeline.LoginAsync("bob");

        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "code_client",
            responseType: "not_existing_response_type",
            scope: "openid",
            redirectUri: "https://code_client/callback",
            state: state,
            nonce: nonce);

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal("unsupported_response_type", _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_null_response_throws_exception()
    {
        await _mockPipeline.LoginAsync("bob");

        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();

        try
        {
            var url = _mockPipeline.CreateAuthorizeUrl(
                clientId: "code_client",
                responseType: null, scope: "openid",
                redirectUri: "https://code_client/callback",
                state: state,
                nonce: nonce);
#if NET6_0
            _mockPipeline.BrowserClient.AllowAutoRedirect = true;
            var response = await _mockPipeline.BrowserClient.GetAsync(url);

            Assert.Equal("unsupported_response_type", _mockPipeline.ErrorMessage.Error);
            return;
#else
            Assert.Fail("Exception should have been thrown");
#endif
        }
        catch (Exception ex)
        {
            Assert.Equal("Parameter is required (Parameter 'response_type')", ex.Message);
        }
    }
}
