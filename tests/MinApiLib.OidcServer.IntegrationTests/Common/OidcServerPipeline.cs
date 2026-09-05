using MinApiLib.OidcServer.Extensions;
using Microsoft.AspNetCore.Http;

namespace MinApiLib.OidcServer.IntegrationTests.Common;

public class OidcServerPipeline : IDisposable
{
    public const string BaseUrl = "https://server";
    public const string LoginPage = BaseUrl + "/account/login";
    public const string ConsentPage = BaseUrl + "/account/consent";
    public const string ErrorPage = BaseUrl + "/home/error";

    public const string DeviceAuthorization = BaseUrl + "/connect/deviceauthorization";
    public const string DiscoveryEndpoint = BaseUrl + "/.well-known/openid-configuration";
    public const string DiscoveryKeysEndpoint = BaseUrl + "/.well-known/openid-configuration/jwks";
    public const string AuthorizeEndpoint = BaseUrl + "/connect/authorize";
    public const string TokenEndpoint = BaseUrl + "/connect/token";
    public const string NonceEndpoint = BaseUrl + "/connect/nonce";
    public const string RevocationEndpoint = BaseUrl + "/connect/revocation";
    public const string UserInfoEndpoint = BaseUrl + "/connect/userinfo";
    public const string IntrospectionEndpoint = BaseUrl + "/connect/introspect";
    public const string IdentityTokenValidationEndpoint = BaseUrl + "/connect/identityTokenValidation";
    public const string EndSessionEndpoint = BaseUrl + "/connect/endsession";
    public const string EndSessionCallbackEndpoint = BaseUrl + "/connect/endsession/callback";
    public const string CheckSessionEndpoint = BaseUrl + "/connect/checksession";

    public const string FederatedSignOutPath = "/signout-oidc";
    public const string FederatedSignOutUrl = BaseUrl + FederatedSignOutPath;

    public OidcServerOptions Options { get; set; }
    public List<Client> Clients { get; set; } = new List<Client>();
    public List<IdentityResource> IdentityScopes { get; set; } = new List<IdentityResource>();
    public List<ApiResource> ApiResources { get; set; } = new List<ApiResource>();
    public List<ApiScope> ApiScopes { get; set; } = new List<ApiScope>();
    public List<TestUser> Users { get; set; } = new List<TestUser>();

    private IHost _host;

    public TestServer Server { get; set; }
    public HttpMessageHandler Handler { get; set; }

    public BrowserClient BrowserClient { get; set; }
    public HttpClient BackChannelClient { get; set; }

    public MockMessageHandler BackChannelMessageHandler { get; set; } = new MockMessageHandler();
    public MockMessageHandler JwtRequestMessageHandler { get; set; } = new MockMessageHandler();

    public event Action<IServiceCollection> OnPreConfigureServices = services => { };
    public event Action<IServiceCollection> OnPostConfigureServices = services => { };
    public event Action<IApplicationBuilder> OnPreConfigure = app => { };
    public event Action<IApplicationBuilder> OnPostConfigure = app => { };

    /// <summary>
    /// Conventions applied to the endpoints mapped in endpoint routing mode.
    /// </summary>
    public Action<OidcServerEndpointConventionBuilder> ConfigureEndpoints { get; set; } = _ => { };

    /// <summary>
    /// Send back-channel logout notifications in the background. Off by default so tests can assert right after
    /// the sign-out request.
    /// </summary>
    public bool BackChannelLogoutInBackground { get; set; }

    public Func<HttpContext, Task<bool>> OnFederatedSignout;

    public void Initialize(string basePath = null, bool enableLogging = false)
    {
        _host?.Dispose();
        _host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(ConfigureServices);
                webHost.Configure(app =>
                {
                    if (basePath != null)
                    {
                        app.Map(basePath, map =>
                        {
                            ConfigureApp(map);
                        });
                    }
                    else
                    {
                        ConfigureApp(app);
                    }
                });

                if (enableLogging)
                {
                    webHost.ConfigureLogging((ctx, b) => b.AddConsole());
                }
            })
            .Start();

        Server = _host.GetTestServer();
        Handler = Server.CreateHandler();

        BrowserClient = new BrowserClient(new BrowserHandler(Handler));
        BackChannelClient = new HttpClient(Handler);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        OnPreConfigureServices(services);

        services.AddAuthentication(opts =>
        {
            opts.AddScheme("external", scheme =>
            {
                scheme.DisplayName = "External";
                scheme.HandlerType = typeof(MockExternalAuthenticationHandler);
            });
        });
        services.AddTransient<MockExternalAuthenticationHandler>(svcs =>
        {
            var handler = new MockExternalAuthenticationHandler(svcs.GetRequiredService<IHttpContextAccessor>());
            if (OnFederatedSignout != null) handler.OnFederatedSignout = OnFederatedSignout;
            return handler;
        });

        services.AddRouting();

        services.AddOidcServer(options =>
        {
            Options = options;
            options.Authentication.SendBackChannelLogoutNotificationsInBackground = BackChannelLogoutInBackground;

            options.Events = new EventsOptions
            {
                RaiseErrorEvents = true,
                RaiseFailureEvents = true,
                RaiseInformationEvents = true,
                RaiseSuccessEvents = true
            };
        })
        .AddInMemoryClients(Clients)
        .AddInMemoryIdentityResources(IdentityScopes)
        .AddInMemoryApiResources(ApiResources)
        .AddInMemoryApiScopes(ApiScopes)
        .AddTestUsers(Users)
        .AddDeveloperSigningCredential(persistKey: false);

        services.AddHttpClient(OidcServerConstants.HttpClients.BackChannelLogoutHttpClient)
            .AddHttpMessageHandler(() => BackChannelMessageHandler);

        services.AddHttpClient(OidcServerConstants.HttpClients.JwtRequestUriHttpClient)
            .AddHttpMessageHandler(() => JwtRequestMessageHandler);

        OnPostConfigureServices(services);
    }

    public void ConfigureApp(IApplicationBuilder app)
    {
        OnPreConfigure(app);

        app.UseRouting();
        app.UseOidcServer();

        app.Map(Constants.UIConstants.DefaultRoutePaths.Login.EnsureLeadingSlash(), path =>
        {
            path.Run(ctx => OnLogin(ctx));
        });
        app.Map(Constants.UIConstants.DefaultRoutePaths.Logout.EnsureLeadingSlash(), path =>
        {
            path.Run(ctx => OnLogout(ctx));
        });
        app.Map(Constants.UIConstants.DefaultRoutePaths.Consent.EnsureLeadingSlash(), path =>
        {
            path.Run(ctx => OnConsent(ctx));
        });
        app.Map(Constants.UIConstants.DefaultRoutePaths.Error.EnsureLeadingSlash(), path =>
        {
            path.Run(ctx => OnError(ctx));
        });

        OnPostConfigure(app);

        app.UseEndpoints(endpoints => ConfigureEndpoints(endpoints.MapOidcServer()));
    }

    public bool LoginWasCalled { get; set; }
    public AuthorizationRequest LoginRequest { get; set; }
    public ClaimsPrincipal Subject { get; set; }
    public bool FollowLoginReturnUrl { get; set; }

    private async Task OnLogin(HttpContext ctx)
    {
        LoginWasCalled = true;
        await ReadLoginRequest(ctx);
        await IssueLoginCookie(ctx);
    }

    private async Task ReadLoginRequest(HttpContext ctx)
    {
        var interaction = ctx.RequestServices.GetRequiredService<IOidcServerInteractionService>();
        LoginRequest = await interaction.GetAuthorizationContextAsync(ctx.Request.Query["returnUrl"].FirstOrDefault());
    }

    private async Task IssueLoginCookie(HttpContext ctx)
    {
        if (Subject != null)
        {
            var props = new AuthenticationProperties();
            await ctx.SignInAsync(Subject, props);
            Subject = null;
            var url = ctx.Request.Query[Options.UserInteraction.LoginReturnUrlParameter].FirstOrDefault();
            if (url != null)
            {
                ctx.Response.Redirect(url);
            }
        }
    }

    public bool LogoutWasCalled { get; set; }
    public LogoutRequest LogoutRequest { get; set; }

    private async Task OnLogout(HttpContext ctx)
    {
        LogoutWasCalled = true;
        await ReadLogoutRequest(ctx);
        await ctx.SignOutAsync();
    }

    private async Task ReadLogoutRequest(HttpContext ctx)
    {
        var interaction = ctx.RequestServices.GetRequiredService<IOidcServerInteractionService>();
        LogoutRequest = await interaction.GetLogoutContextAsync(ctx.Request.Query["logoutId"].FirstOrDefault());
    }

    public bool ConsentWasCalled { get; set; }
    public AuthorizationRequest ConsentRequest { get; set; }
    public ConsentResponse ConsentResponse { get; set; }

    private async Task OnConsent(HttpContext ctx)
    {
        ConsentWasCalled = true;
        await ReadConsentMessage(ctx);
        await CreateConsentResponse(ctx);
    }

    private async Task ReadConsentMessage(HttpContext ctx)
    {
        var interaction = ctx.RequestServices.GetRequiredService<IOidcServerInteractionService>();
        ConsentRequest = await interaction.GetAuthorizationContextAsync(ctx.Request.Query["returnUrl"].FirstOrDefault());
    }

    private async Task CreateConsentResponse(HttpContext ctx)
    {
        if (ConsentRequest != null && ConsentResponse != null)
        {
            var interaction = ctx.RequestServices.GetRequiredService<IOidcServerInteractionService>();
            await interaction.GrantConsentAsync(ConsentRequest, ConsentResponse);
            ConsentResponse = null;

            var url = ctx.Request.Query[Options.UserInteraction.ConsentReturnUrlParameter].FirstOrDefault();
            if (url != null)
            {
                ctx.Response.Redirect(url);
            }
        }
    }

    public bool ErrorWasCalled { get; set; }
    public ErrorMessage ErrorMessage { get; set; }

    private Task OnError(HttpContext ctx)
    {
        ErrorWasCalled = true;
        return ReadErrorMessage(ctx);
    }

    private async Task ReadErrorMessage(HttpContext ctx)
    {
        var interaction = ctx.RequestServices.GetRequiredService<IOidcServerInteractionService>();
        ErrorMessage = await interaction.GetErrorContextAsync(ctx.Request.Query["errorId"].FirstOrDefault());
    }

    public async Task LoginAsync(ClaimsPrincipal subject)
    {
        var old = BrowserClient.AllowAutoRedirect;
        BrowserClient.AllowAutoRedirect = false;

        Subject = subject;
        await BrowserClient.GetAsync(LoginPage);

        BrowserClient.AllowAutoRedirect = old;
    }

    public Task LoginAsync(string subject)
        => LoginAsync(new OidcServerUser(subject).CreatePrincipal());

    public void RemoveLoginCookie()
    {
        BrowserClient.RemoveCookie(BaseUrl, OidcServerConstants.DefaultCookieAuthenticationScheme);
    }
    public void RemoveSessionCookie()
    {
        BrowserClient.RemoveCookie(BaseUrl, OidcServerConstants.DefaultCheckSessionCookieName);
    }
    public Cookie GetSessionCookie()
    {
        return BrowserClient.GetCookie(BaseUrl, OidcServerConstants.DefaultCheckSessionCookieName);
    }

    public string CreateAuthorizeUrl(
        string clientId = null,
        string responseType = null,
        string scope = null,
        string redirectUri = null,
        string state = null,
        string nonce = null,
        string loginHint = null,
        string acrValues = null,
        string responseMode = null,
        string codeChallenge = null,
        string codeChallengeMethod = null,
        string loginHintToken = null,
        Parameters extra = null)
    {
        if (loginHintToken is not null)
        {
            extra ??= [];
            extra.Add("login_hint_token", loginHintToken);
        }

        var url = new RequestUrl(AuthorizeEndpoint).CreateAuthorizeUrl(
            clientId: clientId,
            responseType: responseType,
            scope: scope,
            redirectUri: redirectUri,
            state: state,
            nonce: nonce,
            loginHint: loginHint,
            acrValues: acrValues,
            responseMode: responseMode,
            codeChallenge: codeChallenge,
            codeChallengeMethod: codeChallengeMethod,
            extra: extra);
        return url;
    }

    public AuthorizeResponse ParseAuthorizationResponseUrl(string url)
    {
        return new AuthorizeResponse(url);
    }

    public async Task<AuthorizeResponse> RequestAuthorizationEndpointAsync(
        string clientId,
        string responseType,
        string scope = null,
        string redirectUri = null,
        string state = null,
        string nonce = null,
        string loginHint = null,
        string acrValues = null,
        string responseMode = null,
        string codeChallenge = null,
        string codeChallengeMethod = null,
        Parameters extra = null)
    {
        var old = BrowserClient.AllowAutoRedirect;
        BrowserClient.AllowAutoRedirect = false;

        var url = CreateAuthorizeUrl(clientId, responseType, scope, redirectUri, state, nonce, loginHint, acrValues, responseMode, codeChallenge, codeChallengeMethod, extra: extra);
        var result = await BrowserClient.GetAsync(url);
        Assert.Equal(HttpStatusCode.Found, result.StatusCode);

        BrowserClient.AllowAutoRedirect = old;

        var redirect = result.Headers.Location.ToString();
        if (redirect.StartsWith(OidcServerPipeline.ErrorPage))
        {
            await BrowserClient.GetAsync(redirect);

            return null;
        }

        return new AuthorizeResponse(redirect);
    }

    public void Dispose()
    {
        BrowserClient?.Dispose();
        BackChannelClient?.Dispose();
        Server?.Dispose();
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }

}

public class MockMessageHandler : DelegatingHandler
{
    public bool InvokeWasCalled { get; set; }
    public Func<HttpRequestMessage, Task> OnInvoke { get; set; }
    public HttpResponseMessage Response { get; set; } = new HttpResponseMessage(HttpStatusCode.OK);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        InvokeWasCalled = true;

        if (OnInvoke != null)
        {
            await OnInvoke.Invoke(request);
        }
        return Response;
    }
}

public class MockExternalAuthenticationHandler :
    IAuthenticationHandler,
    IAuthenticationSignInHandler,
    IAuthenticationRequestHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private HttpContext HttpContext => _httpContextAccessor.HttpContext;

    public Func<HttpContext, Task<bool>> OnFederatedSignout =
        async context =>
        {
            await context.SignOutAsync();
            return true;
        };

    public MockExternalAuthenticationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> HandleRequestAsync()
    {
        if (HttpContext.Request.Path == OidcServerPipeline.FederatedSignOutPath)
        {
            return await OnFederatedSignout(HttpContext);
        }

        return false;
    }

    public Task<AuthenticateResult> AuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    public Task ChallengeAsync(AuthenticationProperties properties)
    {
        return Task.CompletedTask;
    }

    public Task ForbidAsync(AuthenticationProperties properties)
    {
        return Task.CompletedTask;
    }

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        return Task.CompletedTask;
    }

    public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
    {
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties properties)
    {
        return Task.CompletedTask;
    }
}
