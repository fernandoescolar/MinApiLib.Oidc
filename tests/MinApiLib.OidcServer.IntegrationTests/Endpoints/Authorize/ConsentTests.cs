namespace MinApiLib.OidcServer.IntegrationTests.Endpoints.Authorize;

public class ConsentTests : IDisposable
{
    private const string Category = "Authorize and consent tests";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public ConsentTests()
    {
        _mockPipeline.Clients.AddRange(new Client[] {
                new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequirePkce = false,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                },
                new Client
                {
                    ClientId = "client2",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequirePkce = false,
                    RequireConsent = true,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client2/callback" },
                },
                new Client
                {
                    ClientId = "client3",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequirePkce = false,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                    RedirectUris = new List<string> { "https://client3/callback" },
                    IdentityProviderRestrictions = new List<string> { "google" }
                }
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
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            });
        _mockPipeline.ApiResources.AddRange(new ApiResource[] {
                new ApiResource
                {
                    Name = "api",
                    Scopes = { "api1", "api2" }
                }
            });

        _mockPipeline.ApiScopes.AddRange(new ApiScope[]
        {
                new ApiScope
                {
                    Name = "api1"
                },
                new ApiScope
                {
                    Name = "api2"
                }
        });

        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_requires_consent_should_show_consent_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce"
        );
        var _ = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ConsentWasCalled);
    }

    [Theory]
    [InlineData((Type)null)]
    [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
    [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
    [Trait("Category", Category)]
    public async Task consent_page_should_have_authorization_params(Type storeType)
    {
        if (storeType != null)
        {
            _mockPipeline.OnPostConfigureServices += services =>
            {
                services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
            };
            _mockPipeline.Initialize();
        }

        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid api1 api2",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "acr_1 acr_2 tenant:tenant_value",
            extra: Parameters.FromObject(new
            {
                display = "popup",
                ui_locales = "ui_locale_value",
                custom_foo = "foo_value"
            })
        );
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.ConsentRequest);
        Assert.Equal("client2", _mockPipeline.ConsentRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.ConsentRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.ConsentRequest.UiLocales);
        Assert.Equal("tenant_value", _mockPipeline.ConsentRequest.Tenant);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.ConsentRequest.AcrValues, strict: true);
        Assert.Contains("custom_foo", _mockPipeline.ConsentRequest.Parameters.AllKeys);
        Assert.Equal("foo_value", _mockPipeline.ConsentRequest.Parameters["custom_foo"]);
        Assert.Equivalent(new string[] { "api2", "openid", "api1" }, _mockPipeline.ConsentRequest.ValidatedResources.RawScopeValues, strict: true);
    }

    [Theory]
    [InlineData((Type)null)]
    [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
    [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
    [Trait("Category", Category)]
    public async Task Consent_response_should_allow_successful_authorization_response(Type storeType)
    {
        if (storeType != null)
        {
            _mockPipeline.OnPostConfigureServices += services =>
            {
                services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
            };
            _mockPipeline.Initialize();
        }

        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.ConsentResponse = new ConsentResponse()
        {
            ScopesValuesConsented = new string[] { "openid", "api2" }
        };
        _mockPipeline.BrowserClient.StopRedirectingAfter = 2;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile api1 api2",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client2/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);
        Assert.Equal("123_state", authorization.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task consent_response_should_reject_modified_request_params()
    {
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.ConsentResponse = new ConsentResponse()
        {
            ScopesValuesConsented = new string[] { "openid", "api2" }
        };
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile api2",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://server/consent", response.Headers.Location.ToString());

        response = await _mockPipeline.BrowserClient.GetAsync(response.Headers.Location.ToString(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/connect/authorize/callback", response.Headers.Location.ToString());

        var modifiedAuthorizeCallback = "https://server" + response.Headers.Location.ToString();
        modifiedAuthorizeCallback = modifiedAuthorizeCallback.Replace("api2", "api1%20api2");

        response = await _mockPipeline.BrowserClient.GetAsync(modifiedAuthorizeCallback, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://server/consent", response.Headers.Location.ToString());
    }

    [Fact()]
    [Trait("Category", Category)]
    public async Task consent_response_missing_required_scopes_should_error()
    {
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.ConsentResponse = new ConsentResponse()
        {
            ScopesValuesConsented = new string[] { "api2" }
        };
        _mockPipeline.BrowserClient.StopRedirectingAfter = 2;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile api1 api2",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client2/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.True(authorization.IsError);
        Assert.Equal("access_denied", authorization.Error);
        Assert.Equal("123_state", authorization.State);
    }
}
