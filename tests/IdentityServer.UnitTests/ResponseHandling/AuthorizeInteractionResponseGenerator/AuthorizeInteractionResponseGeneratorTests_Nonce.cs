namespace IdentityServer.UnitTests.ResponseHandling.AuthorizeInteractionResponseGenerator;

public class AuthorizeInteractionResponseGeneratorTests_Nonce
{
    private const string Category = "AuthorizeInteractionResponseGenerator nonce pattern";
    private const string NewSessionId = "new-session-id";
    private static readonly DateTime Now = new(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly StubClock _clock = new() { UtcNowFunc = () => Now };
    private readonly IAuthorizationNonceStore _nonceStore = Factory.CreateAuthorizationNonceStore();
    private readonly RecordingAuthenticationService _authentication = new();
    private readonly IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator _subject;

    public AuthorizeInteractionResponseGeneratorTests_Nonce()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(_authentication);
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        _subject = new IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator(
            _clock,
            TestLogger.Create<IdentityServer.ResponseHandling.AuthorizeInteractionResponseGenerator>(),
            new MockConsentService(),
            new MockProfileService(),
            _nonceStore,
            new MockHttpContextAccessor { HttpContext = httpContext });
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_nonce_signs_in_subject_and_uses_new_session_id()
    {
        var nonce = await StoreNonceAsync();
        var request = CreateRequest(nonce);

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.False(result.IsError);
        Assert.False(result.IsLogin);
        Assert.Equal("bob", _authentication.SignedInPrincipal.GetSubjectId());
        Assert.Equal("bob", request.Subject.GetSubjectId());
        Assert.Equal(NewSessionId, request.SessionId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Nonce_sign_in_cookie_expiry_uses_time_provider()
    {
        var nonce = await StoreNonceAsync();

        await _subject.ProcessInteractionAsync(CreateRequest(nonce));

        Assert.Equal(new DateTimeOffset(Now).AddDays(1), _authentication.SignInProperties.ExpiresUtc);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Expired_nonce_is_rejected_and_consumed()
    {
        var nonce = await StoreNonceAsync(lifetime: 120, creationTime: Now.AddSeconds(-121));

        var result = await _subject.ProcessInteractionAsync(CreateRequest(nonce));

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
        Assert.Null(_authentication.SignedInPrincipal);
        Assert.Null((await _nonceStore.GetAuthorizationNonceAsync(nonce)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Nonce_within_lifetime_is_accepted()
    {
        var nonce = await StoreNonceAsync(lifetime: 120, creationTime: Now.AddSeconds(-119));

        var result = await _subject.ProcessInteractionAsync(CreateRequest(nonce));

        Assert.False(result.IsError);
        Assert.NotNull(_authentication.SignedInPrincipal);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Nonce_cannot_be_used_twice()
    {
        var nonce = await StoreNonceAsync();
        await _subject.ProcessInteractionAsync(CreateRequest(nonce));

        var result = await _subject.ProcessInteractionAsync(CreateRequest(nonce));

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Target_client_without_local_login_requires_login_after_nonce_sign_in()
    {
        var nonce = await StoreNonceAsync(idp: IdentityServerConstants.LocalIdentityProvider);
        var request = CreateRequest(nonce, new Client { ClientId = "target", EnableLocalLogin = false });

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsLogin);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Target_client_idp_restrictions_require_login_after_nonce_sign_in()
    {
        var nonce = await StoreNonceAsync(idp: "google");
        var request = CreateRequest(nonce, new Client { ClientId = "target", IdentityProviderRestrictions = { "apple" } });

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsLogin);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Max_age_exceeded_requires_login_after_nonce_sign_in()
    {
        var nonce = await StoreNonceAsync(authTime: Now.AddHours(-2));
        var request = CreateRequest(nonce, maxAge: 3600);

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsLogin);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Max_age_exceeded_with_prompt_none_returns_login_required()
    {
        var nonce = await StoreNonceAsync(authTime: Now.AddHours(-2));
        var request = CreateRequest(nonce, maxAge: 3600);
        request.PromptModes = [OidcConstants.PromptModes.None];

        var result = await _subject.ProcessInteractionAsync(request);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.LoginRequired, result.Error);
    }

    private Task<string> StoreNonceAsync(
        int lifetime = 120,
        DateTime? creationTime = null,
        string idp = IdentityServerConstants.LocalIdentityProvider,
        DateTime? authTime = null)
    {
        var subject = new IdentityServerUser("bob")
        {
            IdentityProvider = idp,
            AuthenticationTime = authTime ?? Now.AddMinutes(-5),
            AuthenticationMethods = { OidcConstants.AuthenticationMethods.Password }
        }.CreatePrincipal();

        return _nonceStore.StoreAuthorizationNonceAsync(new AuthorizationNonce
        {
            ClientId = "source",
            CreationTime = creationTime ?? Now,
            Lifetime = lifetime,
            Subject = subject
        });
    }

    private static ValidatedAuthorizeRequest CreateRequest(string nonce, Client client = null, int? maxAge = null) => new()
    {
        ClientId = "target",
        Client = client ?? new Client { ClientId = "target" },
        Subject = Principal.Anonymous,
        AuthenticationContextReferenceClasses = [NonceAuthorization.AcrValue],
        LoginHintToken = nonce,
        ValidatedResources = new ResourceValidationResult(),
        MaxAge = maxAge
    };

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal SignedInPrincipal { get; private set; }

        public AuthenticationProperties SignInProperties { get; private set; }

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            SignedInPrincipal = principal;
            SignInProperties = properties;

            // IdentityServerAuthenticationService assigns the session id on these properties during sign-in.
            properties.SetSessionId(NewSessionId);
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties) => Task.CompletedTask;
    }
}
