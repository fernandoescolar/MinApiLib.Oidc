namespace MinApiLib.Oidc.Server.IntegrationTests.Pipeline;

/// <summary>
/// The server raises the logout, consent and grant revocation events itself.
/// </summary>
public class ServerEventsTests : IDisposable
{
    private const string Category = "Server events";
    private const string LogoutPage = OidcServerPipeline.BaseUrl + "/account/logout";
    private const string RevokePage = OidcServerPipeline.BaseUrl + "/test/revoke";

    private readonly OidcServerPipeline _pipeline = new();
    private readonly CapturingEventSink _sink = new();

    public ServerEventsTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = true,
            AllowedScopes = { "openid", "api" },
            RedirectUris = { "https://client1/callback" },
        });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob", Claims = [new Claim("name", "Bob Loblaw")] });

        _pipeline.OnPostConfigureServices += services => services.AddSingleton<IEventSink>(_sink);
        _pipeline.OnPostConfigure += app => app.Map("/test/revoke", page => page.Run(context =>
            context.RequestServices.GetRequiredService<IOidcServerInteractionService>().RevokeUserConsentAsync("client1")));

        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Signing_out_raises_UserLogoutSuccessEvent()
    {
        await _pipeline.LoginAsync(new OidcServerUser("bob") { DisplayName = "Bob Loblaw" }.CreatePrincipal());

        await _pipeline.BrowserClient.GetAsync(LogoutPage, TestContext.Current.CancellationToken);

        var logout = Assert.Single(_sink.Events.OfType<UserLogoutSuccessEvent>());
        Assert.Equal("bob", logout.SubjectId);
        Assert.Equal("Bob Loblaw", logout.DisplayName);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Signing_out_without_a_user_raises_no_event()
    {
        await _pipeline.BrowserClient.GetAsync(LogoutPage, TestContext.Current.CancellationToken);

        Assert.Empty(_sink.Events.OfType<UserLogoutSuccessEvent>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Granting_consent_raises_ConsentGrantedEvent()
    {
        _pipeline.ConsentResponse = new ConsentResponse { ScopesValuesConsented = ["openid"], RememberConsent = true };

        await AuthorizeAsync();

        var granted = Assert.Single(_sink.Events.OfType<ConsentGrantedEvent>());
        Assert.Equal("bob", granted.SubjectId);
        Assert.Equal("client1", granted.ClientId);
        Assert.Equal(["openid", "api"], granted.RequestedScopes);
        Assert.Equal(["openid"], granted.GrantedScopes);
        Assert.True(granted.ConsentRemembered);
        Assert.Empty(_sink.Events.OfType<ConsentDeniedEvent>());
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Denying_consent_raises_ConsentDeniedEvent(bool withError)
    {
        // Without an error, consenting without the required openid scope is a denial too.
        _pipeline.ConsentResponse = withError
            ? new ConsentResponse { Error = AuthorizationError.AccessDenied }
            : new ConsentResponse { ScopesValuesConsented = ["api"] };

        await AuthorizeAsync();

        var denied = Assert.Single(_sink.Events.OfType<ConsentDeniedEvent>());
        Assert.Equal("bob", denied.SubjectId);
        Assert.Equal("client1", denied.ClientId);
        Assert.Equal(["openid", "api"], denied.RequestedScopes);
        Assert.Empty(_sink.Events.OfType<ConsentGrantedEvent>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Revoking_consent_raises_GrantsRevokedEvent()
    {
        await _pipeline.LoginAsync("bob");

        await _pipeline.BrowserClient.GetAsync(RevokePage, TestContext.Current.CancellationToken);

        var revoked = Assert.Single(_sink.Events.OfType<GrantsRevokedEvent>());
        Assert.Equal("bob", revoked.SubjectId);
        Assert.Equal("client1", revoked.ClientId);
    }

    private async Task AuthorizeAsync()
    {
        await _pipeline.LoginAsync("bob");
        _pipeline.BrowserClient.StopRedirectingAfter = 3;

        var url = _pipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid api",
            redirectUri: "https://client1/callback",
            state: "state",
            nonce: "nonce");
        await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.True(_pipeline.ConsentWasCalled);
    }

    private sealed class CapturingEventSink : IEventSink
    {
        public List<Event> Events { get; } = [];

        public Task PersistAsync(Event evt)
        {
            lock (Events)
            {
                Events.Add(evt);
            }

            return Task.CompletedTask;
        }
    }
}
