namespace IdentityServer.UnitTests.ResponseHandling.AuthorizeInteractionResponseGenerator;

public class CustomAuthorizeInteractionResponseGenerator : IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator
{
    public CustomAuthorizeInteractionResponseGenerator(TimeProvider clock, ILogger<IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator> logger, IConsentService consent, IProfileService profile, IAuthorizationNonceStore nonceStore, IHttpContextAccessor httpContextAccessor)
        : base(clock, logger, consent, profile, nonceStore, httpContextAccessor)
    {
    }

    public InteractionResponse ProcessLoginResponse { get; set; }
    protected internal override Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request)
    {
        if (ProcessLoginResponse != null)
        {
            return Task.FromResult(ProcessLoginResponse);
        }

        return base.ProcessLoginAsync(request);
    }

    public InteractionResponse ProcessConsentResponse { get; set; }
    protected internal override Task<InteractionResponse> ProcessConsentAsync(ValidatedAuthorizeRequest request, ConsentResponse consent = null)
    {
        if (ProcessConsentResponse != null)
        {
            return Task.FromResult(ProcessConsentResponse);
        }
        return base.ProcessConsentAsync(request, consent);
    }
}

public class AuthorizeInteractionResponseGeneratorTests_Custom
{
    private CustomAuthorizeInteractionResponseGenerator _subject;
    private MockConsentService _mockConsentService = new MockConsentService();
    private MockHttpContextAccessor _mockHttpContextAccessor = new MockHttpContextAccessor();
    private StubClock _clock = new StubClock();

    public AuthorizeInteractionResponseGeneratorTests_Custom()
    {
        _subject = new CustomAuthorizeInteractionResponseGenerator(
            _clock,
            TestLogger.Create<IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator>(),
            _mockConsentService,
            new MockProfileService(),
            Factory.CreateAuthorizationNonceStore(),
            _mockHttpContextAccessor);
    }

    [Fact]
    public async Task ProcessInteractionAsync_with_overridden_login_returns_redirect_should_return_redirect()
    {
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = "foo",
            Subject = new IdentityServerUser("123")
            {
                IdentityProvider = IdentityServerConstants.LocalIdentityProvider
            }.CreatePrincipal(),
            Client = new Client
            {
            },
        };

        _subject.ProcessLoginResponse = new InteractionResponse
        {
            RedirectUrl = "/custom"
        };

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsRedirect);
        Assert.Equal("/custom", result.RedirectUrl);
    }

    [Fact]
    public async Task ProcessInteractionAsync_with_prompt_none_and_login_returns_login_should_return_error()
    {
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = "foo",
            Subject = new IdentityServerUser("123")
            {
                IdentityProvider = IdentityServerConstants.LocalIdentityProvider
            }.CreatePrincipal(),
            Client = new Client
            {
            },
            PromptModes = new[] { PromptModes.None },
        };

        _subject.ProcessLoginResponse = new InteractionResponse
        {
            IsLogin = true
        };

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsError);
        Assert.Equal("login_required", result.Error);
    }

    [Fact]
    public async Task ProcessInteractionAsync_with_prompt_none_and_login_returns_redirect_should_return_error()
    {
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = "foo",
            Subject = new IdentityServerUser("123")
            {
                IdentityProvider = IdentityServerConstants.LocalIdentityProvider
            }.CreatePrincipal(),
            Client = new Client
            {
            },
            PromptModes = new[] { PromptModes.None },
        };

        _subject.ProcessLoginResponse = new InteractionResponse
        {
            RedirectUrl = "/custom"
        };

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsError);
        Assert.Equal("interaction_required", result.Error);
        Assert.Null(result.RedirectUrl);
    }

    [Fact]
    public async Task ProcessInteractionAsync_with_prompt_none_and_consent_returns_consent_should_return_error()
    {
        var request = new ValidatedAuthorizeRequest
        {
            ClientId = "foo",
            Subject = new IdentityServerUser("123")
            {
                IdentityProvider = IdentityServerConstants.LocalIdentityProvider
            }.CreatePrincipal(),
            Client = new Client
            {
            },
            PromptModes = new[] { PromptModes.None },
        };

        _subject.ProcessConsentResponse = new InteractionResponse
        {
            IsConsent = true
        };

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsError);
        Assert.Equal("consent_required", result.Error);
    }
}
