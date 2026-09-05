namespace IdentityServer.UnitTests.ResponseHandling.AuthorizeInteractionResponseGenerator;

public class AuthorizeInteractionResponseGeneratorTests_Consent
{
    private IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator _subject;
    private MockConsentService _mockConsent = new MockConsentService();
    private MockProfileService _fakeUserService = new MockProfileService();
    private MockHttpContextAccessor _mockHttpContextAccessor = new MockHttpContextAccessor();

    private void RequiresConsent(bool value)
    {
        _mockConsent.RequiresConsentResult = value;
    }

    private void AssertUpdateConsentNotCalled()
    {
        Assert.Null(_mockConsent.ConsentClient);
        Assert.Null(_mockConsent.ConsentSubject);
        Assert.Null(_mockConsent.ConsentScopes);
    }

    private void AssertUpdateConsentCalled(Client client, ClaimsPrincipal user, params string[] scopes)
    {
        Assert.Same(client, _mockConsent.ConsentClient);
        Assert.Same(user, _mockConsent.ConsentSubject);
        Assert.Equivalent(scopes, _mockConsent.ConsentScopes, strict: true);
    }

    private static IEnumerable<IdentityResource> GetIdentityScopes()
    {
        return new IdentityResource[]
        {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
        };
    }

    private static IEnumerable<ApiResource> GetApiResources()
    {
        return new ApiResource[]
        {
                new ApiResource
                {
                    Name = "api",
                    Scopes = { "read", "write", "forbidden" }
                }
         };
    }

    private static IEnumerable<ApiScope> GetScopes()
    {
        return new ApiScope[]
        {
                new ApiScope
                {
                    Name = "read",
                    DisplayName = "Read data",
                    Emphasize = false
                },
                new ApiScope
                {
                    Name = "write",
                    DisplayName = "Write data",
                    Emphasize = true
                },
                new ApiScope
                {
                    Name = "forbidden",
                    DisplayName = "Forbidden scope",
                    Emphasize = true
                }
         };
    }

    public AuthorizeInteractionResponseGeneratorTests_Consent()
    {
        _subject = new IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator(
            new StubClock(),
            TestLogger.Create<IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator>(),
            _mockConsent,
            _fakeUserService,
            Factory.CreateAuthorizationNonceStore(),
            _mockHttpContextAccessor);
    }

    private static ResourceValidationResult GetValidatedResources(params string[] scopes)
    {
        var resources = new Resources(GetIdentityScopes(), GetApiResources(), GetScopes());
        return new ResourceValidationResult(resources).Filter(scopes);
    }

    [Fact]
    public async Task ProcessConsentAsync_NullRequest_ThrowsAsync()
    {
        var act = () => _subject.ProcessConsentAsync(null, new ConsentResponse());

        Assert.Equal("request", (await Assert.ThrowsAnyAsync<ArgumentNullException>(act)).ParamName);
    }

    [Fact]
    public Task ProcessConsentAsync_AllowsNullConsent()
    {
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.Consent },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        return _subject.ProcessConsentAsync(request, null);
    }

    [Fact]
    public async Task ProcessConsentAsync_PromptModeIsLogin_Throws()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.Login },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };

        var act = () => _subject.ProcessConsentAsync(request);

        Assert.Contains("PromptMode", (await Assert.ThrowsAnyAsync<ArgumentException>(act)).Message);
    }

    [Fact]
    public async Task ProcessConsentAsync_PromptModeIsSelectAccount_ThrowsAsync()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.SelectAccount },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };

        var act = () => _subject.ProcessConsentAsync(request);

        Assert.Contains("PromptMode", (await Assert.ThrowsAnyAsync<ArgumentException>(act)).Message);
    }

    [Fact]
    public async Task ProcessConsentAsync_RequiresConsentButPromptModeIsNone_ReturnsErrorResult()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.None },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var result = await _subject.ProcessConsentAsync(request);

        Assert.False(request.WasConsentShown);
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.ConsentRequired, result.Error);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_PromptModeIsConsent_NoPriorConsent_ReturnsConsentResult()
    {
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.Consent },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var result = await _subject.ProcessConsentAsync(request);
        Assert.False(request.WasConsentShown);
        Assert.True(result.IsConsent);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_NoPromptMode_ConsentServiceRequiresConsent_NoPriorConsent_ReturnsConsentResult()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.Consent },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var result = await _subject.ProcessConsentAsync(request);
        Assert.False(request.WasConsentShown);
        Assert.True(result.IsConsent);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_PromptModeIsConsent_ConsentNotGranted_ReturnsErrorResult()
    {
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            PromptModes = new[] { OidcConstants.PromptModes.Consent },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };

        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        Assert.True(request.WasConsentShown);
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.AccessDenied, result.Error);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_NoPromptMode_ConsentServiceRequiresConsent_ConsentNotGranted_ReturnsErrorResult()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        Assert.True(request.WasConsentShown);
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.AccessDenied, result.Error);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_NoPromptMode_ConsentServiceRequiresConsent_ConsentGrantedButMissingRequiredScopes_ReturnsErrorResult()
    {
        RequiresConsent(true);
        var client = new Client { };
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
            Client = client
        };

        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { "read" }
        };

        var result = await _subject.ProcessConsentAsync(request, consent);
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.AccessDenied, result.Error);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_NoPromptMode_ConsentServiceRequiresConsent_ConsentGranted_ScopesSelected_ReturnsConsentResult()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            Client = new Client
            {
                AllowRememberConsent = false
            },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { "openid", "read" }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        Assert.Single(request.ValidatedResources.Resources.IdentityResources);
        Assert.Single(request.ValidatedResources.Resources.ApiScopes);
        Assert.Equal("openid", request.ValidatedResources.Resources.IdentityResources.Select(x => x.Name).First());
        Assert.Equal("read", request.ValidatedResources.Resources.ApiScopes.First().Name);
        Assert.True(request.WasConsentShown);
        Assert.False(result.IsConsent);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_PromptModeConsent_ConsentGranted_ScopesSelected_ReturnsConsentResult()
    {
        RequiresConsent(true);
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            Client = new Client
            {
                AllowRememberConsent = false
            },
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { "openid", "read" }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        Assert.Single(request.ValidatedResources.Resources.IdentityResources);
        Assert.Single(request.ValidatedResources.Resources.ApiScopes);
        Assert.Equal("read", request.ValidatedResources.Resources.ApiScopes.First().Name);
        Assert.True(request.WasConsentShown);
        Assert.False(result.IsConsent);
        AssertUpdateConsentNotCalled();
    }

    [Fact]
    public async Task ProcessConsentAsync_AllowConsentSelected_SavesConsent()
    {
        RequiresConsent(true);
        var client = new Client { AllowRememberConsent = true };
        var user = new ClaimsPrincipal();
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            Client = client,
            Subject = user,
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var consent = new ConsentResponse
        {
            RememberConsent = true,
            ScopesValuesConsented = new string[] { "openid", "read" }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        AssertUpdateConsentCalled(client, user, "openid", "read");
    }

    [Fact]
    public async Task ProcessConsentAsync_NotRememberingConsent_DoesNotSaveConsent()
    {
        RequiresConsent(true);
        var client = new Client { AllowRememberConsent = true };
        var user = new ClaimsPrincipal();
        var request = new ValidatedAuthorizeRequest()
        {
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            State = "12345",
            RedirectUri = "https://client.com/callback",
            Client = client,
            Subject = user,
            RequestedScopes = new List<string> { "openid", "read", "write" },
            ValidatedResources = GetValidatedResources("openid", "read", "write"),
        };
        var consent = new ConsentResponse
        {
            RememberConsent = false,
            ScopesValuesConsented = new string[] { "openid", "read" }
        };
        var result = await _subject.ProcessConsentAsync(request, consent);
        AssertUpdateConsentCalled(client, user);
    }
}
