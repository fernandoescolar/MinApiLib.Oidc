namespace IdentityServer.UnitTests.ResponseHandling;

public class UserInfoResponseGeneratorTests
{
    private UserInfoResponseGenerator _subject;
    private MockProfileService _mockProfileService = new MockProfileService();
    private ClaimsPrincipal _user;
    private Client _client;

    private InMemoryResourcesStore _resourceStore;
    private List<IdentityResource> _identityResources = new List<IdentityResource>();
    private List<ApiResource> _apiResources = new List<ApiResource>();
    private List<ApiScope> _apiScopes = new List<ApiScope>();

    public UserInfoResponseGeneratorTests()
    {
        _client = new Client
        {
            ClientId = "client"
        };

        _user = new IdentityServerUser("bob")
        {
            AdditionalClaims =
                {
                    new Claim("foo", "foo1"),
                    new Claim("foo", "foo2"),
                    new Claim("bar", "bar1"),
                    new Claim("bar", "bar2")
                }
        }.CreatePrincipal();

        _resourceStore = new InMemoryResourcesStore(_identityResources, _apiResources, _apiScopes);
        _subject = new UserInfoResponseGenerator(_mockProfileService, _resourceStore, TestLogger.Create<UserInfoResponseGenerator>());
    }

    [Fact]
    public async Task GetRequestedClaimTypesAsync_when_no_scopes_requested_should_return_empty_claim_types()
    {
        var resources = await _subject.GetRequestedResourcesAsync(null);
        var claims = await _subject.GetRequestedClaimTypesAsync(resources);
        Assert.Equivalent(new string[] { }, claims, strict: true);
    }

    [Fact]
    public async Task GetRequestedClaimTypesAsync_should_return_correct_identity_claims()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "c1", "c2" }));
        _identityResources.Add(new IdentityResource("id2", new[] { "c2", "c3" }));

        var resources = await _subject.GetRequestedResourcesAsync(new[] { "id1", "id2", "id3" });
        var claims = await _subject.GetRequestedClaimTypesAsync(resources);
        Assert.Equivalent(new string[] { "c1", "c2", "c3" }, claims, strict: true);
    }

    [Fact]
    public async Task GetRequestedClaimTypesAsync_should_only_return_enabled_identity_claims()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "c1", "c2" }) { Enabled = false });
        _identityResources.Add(new IdentityResource("id2", new[] { "c2", "c3" }));

        var resources = await _subject.GetRequestedResourcesAsync(new[] { "id1", "id2", "id3" });
        var claims = await _subject.GetRequestedClaimTypesAsync(resources);
        Assert.Equivalent(new string[] { "c2", "c3" }, claims, strict: true);
    }

    [Fact]
    public async Task ProcessAsync_should_call_profile_service_with_requested_claim_types()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "foo" }));
        _identityResources.Add(new IdentityResource("id2", new[] { "bar" }));

        var result = new UserInfoRequestValidationResult
        {
            Subject = _user,
            TokenValidationResult = new TokenValidationResult
            {
                Claims = new List<Claim>
                    {
                        { new Claim("scope", "id1") },
                        { new Claim("scope", "id2") },
                        { new Claim("scope", "id3") }
                    },
                Client = _client
            }
        };

        var claims = await _subject.ProcessAsync(result);

        Assert.True(_mockProfileService.GetProfileWasCalled);
        Assert.Equivalent(new[] { "foo", "bar" }, _mockProfileService.ProfileContext.RequestedClaimTypes, strict: true);
    }

    [Fact]
    public async Task ProcessAsync_should_return_claims_issued_by_profile_service()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "foo" }));
        _identityResources.Add(new IdentityResource("id2", new[] { "bar" }));

        var address = new
        {
            street_address = "One Hacker Way",
            locality = "Heidelberg",
            postal_code = 69118,
            country = "Germany"
        };

        _mockProfileService.ProfileClaims = new[]
        {
                new Claim("email", "fred@gmail.com"),
                new Claim("name", "fred jones"),
                new Claim("address", @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServerConstants.ClaimValueTypes.Json),
                new Claim("address2", JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
            };

        var result = new UserInfoRequestValidationResult
        {
            Subject = _user,
            TokenValidationResult = new TokenValidationResult
            {
                Claims = new List<Claim>
                    {
                        { new Claim("scope", "id1") },
                        { new Claim("scope", "id2") },
                        { new Claim("scope", "id3") }
                    },
                Client = _client
            }
        };

        var claims = await _subject.ProcessAsync(result);

        Assert.Contains("email", claims);
        Assert.Equal("fred@gmail.com", claims["email"]);
        Assert.Contains("name", claims);
        Assert.Equal("fred jones", claims["name"]);

        Assert.Contains("address", claims);
        Assert.Equal("{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", claims["address"]);

        Assert.Contains("address2", claims);
        Assert.Equal("{\"street_address\":\"One Hacker Way\",\"locality\":\"Heidelberg\",\"postal_code\":69118,\"country\":\"Germany\"}", claims["address2"].ToString());
    }

    [Fact]
    public async Task ProcessAsync_should_return_sub_from_user()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "foo" }));
        _identityResources.Add(new IdentityResource("id2", new[] { "bar" }));

        var result = new UserInfoRequestValidationResult
        {
            Subject = _user,
            TokenValidationResult = new TokenValidationResult
            {
                Claims = new List<Claim>
                    {
                        { new Claim("scope", "id1") },
                        { new Claim("scope", "id2") },
                        { new Claim("scope", "id3") }
                    },
                Client = _client
            }
        };

        var claims = await _subject.ProcessAsync(result);

        Assert.Contains("sub", claims);
        Assert.Equal("bob", claims["sub"]);
    }

    [Fact]
    public async Task ProcessAsync_should_throw_if_incorrect_sub_issued_by_profile_serviceAsync()
    {
        _identityResources.Add(new IdentityResource("id1", new[] { "foo" }));
        _identityResources.Add(new IdentityResource("id2", new[] { "bar" }));
        _mockProfileService.ProfileClaims = new[]
        {
                new Claim("sub", "fred")
            };

        var result = new UserInfoRequestValidationResult
        {
            Subject = _user,
            TokenValidationResult = new TokenValidationResult
            {
                Claims = new List<Claim>
                    {
                        { new Claim("scope", "id1") },
                        { new Claim("scope", "id2") },
                        { new Claim("scope", "id3") }
                    },
                Client = _client
            }
        };

        var act = () => _subject.ProcessAsync(result);

        Assert.Contains("subject", (await Assert.ThrowsAnyAsync<InvalidOperationException>(act)).Message);
    }

}
