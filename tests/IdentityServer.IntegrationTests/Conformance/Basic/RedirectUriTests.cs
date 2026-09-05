namespace IdentityServer.IntegrationTests.Conformance.Basic;

public class RedirectUriTests : IDisposable
{
    private const string Category = "Conformance.Basic.RedirectUriTests";

    private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public RedirectUriTests()
    {
        _mockPipeline.Initialize();

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
                    "https://code_client/callback",
                    "https://code_client/callback?foo=bar&baz=quux"
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
    public async Task Reject_redirect_uri_not_matching_registered_redirect_uri()
    {
        await _mockPipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var state = Guid.NewGuid().ToString();

        var url = _mockPipeline.CreateAuthorizeUrl(
                       clientId: "code_client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://bad",
                       state: state,
                       nonce: nonce);
        var _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Reject_request_without_redirect_uri_when_multiple_registered()
    {
        await _mockPipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var state = Guid.NewGuid().ToString();

        var url = _mockPipeline.CreateAuthorizeUrl(
                      clientId: "code_client",
                      responseType: "code",
                      scope: "openid", redirectUri: null,
                      state: state,
                      nonce: nonce);
        var _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Preserves_query_parameters_in_redirect_uri()
    {
        await _mockPipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var state = Guid.NewGuid().ToString();

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _mockPipeline.CreateAuthorizeUrl(
                       clientId: "code_client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://code_client/callback?foo=bar&baz=quux",
                       state: state,
                       nonce: nonce);
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://code_client/callback?", response.Headers.Location.ToString());
        var authorization = _mockPipeline.ParseAuthorizationResponseUrl(response.Headers.Location.ToString());
        Assert.NotNull(authorization.Code);
        Assert.Equal(state, authorization.State);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(response.Headers.Location.Query);
        Assert.Equal("bar", query["foo"].ToString());
        Assert.Equal("quux", query["baz"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Rejects_redirect_uri_when_query_parameter_does_not_match()
    {
        await _mockPipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();
        var state = Guid.NewGuid().ToString();

        var url = _mockPipeline.CreateAuthorizeUrl(
                       clientId: "code_client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://code_client/callback?baz=quux&foo=bar",
                       state: state,
                       nonce: nonce);
        var _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
    }
}
