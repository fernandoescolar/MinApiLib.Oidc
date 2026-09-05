namespace IdentityServer.UnitTests.Services.Default;

public class DefaultTokenServiceTests
{
    private DefaultTokenService _subject;

    MockClaimsService _mockClaimsService = new();
    MockReferenceTokenStore _mockReferenceTokenStore = new();
    MockTokenCreationService _mockTokenCreationService = new();
    DefaultHttpContext _httpContext = new();
    StubClock _clock = new();
    MockKeyMaterialService _mockKeyMaterialService = new();
    IdentityServerOptions _options = new();

    public DefaultTokenServiceTests()
    {
        _options.IssuerUri = "https://test.identityserver.io";

        var svcs = new SimpleServiceProvider();
        svcs.Add(_options);
        _httpContext.RequestServices = svcs;

        _subject = new DefaultTokenService(
            _mockClaimsService,
            _mockReferenceTokenStore,
            _mockTokenCreationService,
            new HttpContextAccessor { HttpContext = _httpContext },
            new DefaultIssuerNameService(_options, new HttpContextAccessor { HttpContext = _httpContext }),
            _clock,
            _mockKeyMaterialService,
            _options,
            TestLogger.Create<DefaultTokenService>());
    }

    [Fact]
    public async Task CreateAccessTokenAsync_should_include_aud_for_each_ApiResource()
    {
        var request = new TokenCreationRequest
        {
            ValidatedResources = new ResourceValidationResult()
            {
                Resources = new Resources()
                {
                    ApiResources =
                        {
                            new ApiResource("api1"){ Scopes = { "scope1" } },
                            new ApiResource("api2"){ Scopes = { "scope2" } },
                            new ApiResource("api3"){ Scopes = { "scope3" } },
                        },
                },
                ParsedScopes =
                    {
                        new ParsedScopeValue("scope1"),
                        new ParsedScopeValue("scope2"),
                        new ParsedScopeValue("scope3"),
                    }
            },
            ValidatedRequest = new ValidatedRequest()
            {
                Client = new Client { }
            }
        };

        var result = await _subject.CreateAccessTokenAsync(request);

        Assert.Equal(3, result.Audiences.Count);
        Assert.Equivalent(new[] { "api1", "api2", "api3" }, result.Audiences, strict: true);
    }

    [Fact]
    public async Task CreateAccessTokenAsync_when_no_apiresources_should_include_static_aud()
    {
        var request = new TokenCreationRequest
        {
            ValidatedResources = new ResourceValidationResult()
            {
                Resources = new Resources()
                {
                    ApiScopes =
                        {
                            new ApiScope("scope1"),
                            new ApiScope("scope2"),
                            new ApiScope("scope3"),
                        },
                },
                ParsedScopes =
                    {
                        new ParsedScopeValue("scope1"),
                        new ParsedScopeValue("scope2"),
                        new ParsedScopeValue("scope3"),
                    }
            },
            ValidatedRequest = new ValidatedRequest()
            {
                Client = new Client { }
            }
        };

        var result = await _subject.CreateAccessTokenAsync(request);


        // RFC 9068 section 2.2: an access token always has an audience; without API resources the static one is used.

        Assert.Equivalent(new[] { "https://test.identityserver.io/resources" }, result.Audiences, strict: true);
    }

    [Fact]
    public async Task CreateAccessTokenAsync_when_no_session_should_not_include_sid()
    {
        var request = new TokenCreationRequest
        {
            ValidatedResources = new ResourceValidationResult(),
            ValidatedRequest = new ValidatedRequest()
            {
                Client = new Client { },
                SessionId = null
            }
        };

        var result = await _subject.CreateAccessTokenAsync(request);

        Assert.Null(result.Claims.SingleOrDefault(x => x.Type == JwtClaimTypes.SessionId));
    }
    [Fact]
    public async Task CreateAccessTokenAsync_when_session_should_include_sid()
    {
        var request = new TokenCreationRequest
        {
            ValidatedResources = new ResourceValidationResult(),
            ValidatedRequest = new ValidatedRequest()
            {
                Client = new Client { },
                SessionId = "123"
            }
        };

        var result = await _subject.CreateAccessTokenAsync(request);

        Assert.Equal("123", result.Claims.SingleOrDefault(x => x.Type == JwtClaimTypes.SessionId).Value);
    }
}
