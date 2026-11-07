namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.DeviceAuthorization;

public class DeviceFlowInteractionTests : IDisposable
{
    private const string Category = "Device flow interaction";
    private const string VerificationPage = OidcServerPipeline.BaseUrl + "/test/device";

    private readonly OidcServerPipeline _pipeline = new();
    private ConsentResponse _consent;
    private DeviceFlowInteractionResult _result;
    private bool _initialized;
    private readonly List<Event> _events = [];

    public DeviceFlowInteractionTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "device",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.DeviceFlow,
            AllowedScopes = { "openid", "api", "orders" }
        });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.ApiScopes.Add(new ApiScope("orders") { Required = true });
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });

        _pipeline.OnPostConfigureServices += services => services.AddSingleton<IEventSink>(new CapturingEventSink(_events));

        // The host's verification page: it completes the request for the signed-in user with the test's consent.
        _pipeline.OnPostConfigure += app => app.Map("/test/device", page => page.Run(async context =>
        {
            var interaction = context.RequestServices.GetRequiredService<IDeviceFlowInteractionService>();
            _result = await interaction.HandleRequestAsync(context.Request.Query["userCode"].ToString(), _consent);
        }));
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Granted_request_issues_the_consented_scopes()
    {
        var device = await StartAsync();

        await CompleteAsync(device, new ConsentResponse { ScopesValuesConsented = ["openid", "api"] });
        var token = await PollAsync(device);

        Assert.False(_result.IsError);
        Assert.False(_result.IsAccessDenied);
        Assert.True(token.StatusCode == HttpStatusCode.OK, token.Body.ToJsonString());
        Assert.Equal("openid api", token.Body["scope"]!.GetValue<string>());

        var granted = Assert.Single(_events.OfType<ConsentGrantedEvent>());
        Assert.Equal("bob", granted.SubjectId);
        Assert.Equal("device", granted.ClientId);
        Assert.Equal(["openid", "api"], granted.RequestedScopes);
        Assert.Equal(["openid", "api"], granted.GrantedScopes);
        Assert.False(granted.ConsentRemembered);
        Assert.Empty(_events.OfType<ConsentDeniedEvent>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Denied_request_answers_access_denied_to_the_device()
    {
        var device = await StartAsync();

        await CompleteAsync(device, new ConsentResponse { Error = AuthorizationError.AccessDenied });
        var token = await PollAsync(device);

        Assert.False(_result.IsError);
        Assert.True(_result.IsAccessDenied);
        Assert.Equal(HttpStatusCode.BadRequest, token.StatusCode);
        Assert.Equal(OidcConstants.TokenErrors.AccessDenied, token.Body["error"]!.GetValue<string>());

        var denied = Assert.Single(_events.OfType<ConsentDeniedEvent>());
        Assert.Equal("bob", denied.SubjectId);
        Assert.Equal("device", denied.ClientId);
        Assert.Equal(["openid", "api"], denied.RequestedScopes);
        Assert.Empty(_events.OfType<ConsentGrantedEvent>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Error_denies_the_request_even_with_consented_scopes()
    {
        var device = await StartAsync();

        await CompleteAsync(device, new ConsentResponse { Error = AuthorizationError.AccessDenied, ScopesValuesConsented = ["openid", "api"] });
        var token = await PollAsync(device);

        Assert.True(_result.IsAccessDenied);
        Assert.Equal(OidcConstants.TokenErrors.AccessDenied, token.Body["error"]!.GetValue<string>());
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("openid api")]
    [InlineData("api orders")]
    public async Task Leaving_out_a_required_scope_denies_the_request(string consented)
    {
        var device = await StartAsync("openid api orders");

        await CompleteAsync(device, new ConsentResponse { ScopesValuesConsented = consented.Split(' ') });
        var token = await PollAsync(device);

        Assert.True(_result.IsAccessDenied);
        Assert.Equal(OidcConstants.TokenErrors.AccessDenied, token.Body["error"]!.GetValue<string>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Optional_scopes_can_be_left_out()
    {
        var device = await StartAsync("openid api orders");

        await CompleteAsync(device, new ConsentResponse { ScopesValuesConsented = ["openid", "orders"] });
        var token = await PollAsync(device);

        Assert.False(_result.IsAccessDenied);
        Assert.True(token.StatusCode == HttpStatusCode.OK, token.Body.ToJsonString());
        Assert.Equal("openid orders", token.Body["scope"]!.GetValue<string>());
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Granted_request_remembers_the_consent_when_asked(bool remember)
    {
        var device = await StartAsync();

        await CompleteAsync(device, new ConsentResponse { ScopesValuesConsented = ["openid", "api"], RememberConsent = remember });

        var store = _pipeline.Server.Services.GetRequiredService<IUserConsentStore>();
        var consent = await store.GetUserConsentAsync("bob", "device");
        if (remember)
        {
            Assert.Equal(["api", "openid"], consent!.Scopes.Order(StringComparer.Ordinal));
        }
        else
        {
            Assert.Null(consent);
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Denied_request_does_not_remember_the_consent()
    {
        var device = await StartAsync();

        await CompleteAsync(device, new ConsentResponse { Error = AuthorizationError.AccessDenied, RememberConsent = true });

        var store = _pipeline.Server.Services.GetRequiredService<IUserConsentStore>();
        Assert.Null(await store.GetUserConsentAsync("bob", "device"));
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(0, false)]
    [InlineData(5, true)]
    public async Task Failed_user_code_lookups_are_limited_unless_the_limit_is_zero(int limit, bool blocked)
    {
        _pipeline.OnPostConfigureServices += services =>
            services.Configure<OidcServerOptions>(options => options.DeviceFlow.UserCodeMaxFailedAttempts = limit);
        var device = await StartAsync();
        var consent = new ConsentResponse { ScopesValuesConsented = ["openid"] };

        for (var i = 0; i < 6; i++)
        {
            await CompleteAsync(JsonNode.Parse(@"{""user_code"": ""wrong""}"), consent);
            Assert.True(_result.IsError);
        }

        await CompleteAsync(device, consent);

        Assert.Equal(blocked, _result.IsError);
    }

    private async Task<JsonNode> StartAsync(string scope = "openid api")
    {
        if (!_initialized)
        {
            _pipeline.Initialize();
            _initialized = true;
        }

        var response = await _pipeline.BackChannelClient.PostAsync(OidcServerPipeline.DeviceAuthorization, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", "device" },
            { "client_secret", "secret" },
            { "scope", scope },
        }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }

    private async Task CompleteAsync(JsonNode device, ConsentResponse consent)
    {
        await _pipeline.LoginAsync("bob");
        _consent = consent;

        var userCode = device["user_code"]!.GetValue<string>();
        var response = await _pipeline.BrowserClient.GetAsync(VerificationPage + "?userCode=" + Uri.EscapeDataString(userCode), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_result);
    }

    private async Task<(HttpStatusCode StatusCode, JsonNode Body)> PollAsync(JsonNode device)
    {
        var response = await _pipeline.BackChannelClient.PostAsync(OidcServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", OidcConstants.GrantTypes.DeviceCode },
            { "device_code", device["device_code"]!.GetValue<string>() },
            { "client_id", "device" },
            { "client_secret", "secret" },
        }), TestContext.Current.CancellationToken);

        return (response.StatusCode, JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!);
    }

    private sealed class CapturingEventSink(List<Event> events) : IEventSink
    {
        public Task PersistAsync(Event evt)
        {
            lock (events)
            {
                events.Add(evt);
            }

            return Task.CompletedTask;
        }
    }
}
