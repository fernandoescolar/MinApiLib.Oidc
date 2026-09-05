namespace IdentityServer.UnitTests.Services.Default;

public class DefaultDeviceFlowInteractionServiceTests
{
    private const string Category = "Device flow interaction";
    private const string ValidUserCode = "123456789";

    private readonly IdentityServerOptions _options = new();
    private readonly MockUserSession _session = new() { User = new IdentityServerUser("bob").CreatePrincipal() };
    private readonly IDeviceFlowCodeService _devices = new DefaultDeviceFlowCodeService(new InMemoryDeviceFlowStore(), new DefaultHandleGenerationService());
    private readonly DefaultDeviceFlowInteractionService _subject;

    public DefaultDeviceFlowInteractionServiceTests()
    {
        _subject = new DefaultDeviceFlowInteractionService(
            new InMemoryClientStore([new Client { ClientId = "device", AllowedGrantTypes = GrantTypes.DeviceFlow }]),
            _session,
            _devices,
            new InMemoryResourcesStore(TestScopes.GetIdentity(), TestScopes.GetApis(), TestScopes.GetScopes()),
            new DefaultScopeParser(TestLogger.Create<DefaultScopeParser>()),
            new TestCache(),
            new MockHttpContextAccessor(),
            _options,
            new StubClock(),
            TestLogger.Create<DefaultDeviceFlowInteractionService>());
    }

    private Task StoreDeviceCodeAsync() => _devices.StoreDeviceAuthorizationAsync(ValidUserCode, new DeviceCode
    {
        ClientId = "device",
        CreationTime = DateTime.UtcNow,
        Lifetime = 300,
        RequestedScopes = ["openid"]
    });

    [Fact]
    [Trait("Category", Category)]
    public async Task Failed_user_code_lookups_are_limited()
    {
        await StoreDeviceCodeAsync();

        for (var i = 0; i < 5; i++)
        {
            Assert.Null((await _subject.GetAuthorizationContextAsync("000000000")));
        }

        // Locked out for the rest of the window, even with the right code.
        Assert.Null((await _subject.GetAuthorizationContextAsync(ValidUserCode)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_user_code_is_found_below_the_limit()
    {
        await StoreDeviceCodeAsync();

        for (var i = 0; i < 4; i++)
        {
            await _subject.GetAuthorizationContextAsync("000000000");
        }

        Assert.NotNull((await _subject.GetAuthorizationContextAsync(ValidUserCode)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Limit_can_be_disabled()
    {
        _options.DeviceFlow.UserCodeMaxFailedAttempts = 0;
        await StoreDeviceCodeAsync();

        for (var i = 0; i < 10; i++)
        {
            await _subject.GetAuthorizationContextAsync("000000000");
        }

        Assert.NotNull((await _subject.GetAuthorizationContextAsync(ValidUserCode)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Consenting_to_scopes_that_were_not_requested_fails()
    {
        await StoreDeviceCodeAsync();

        var result = await _subject.HandleRequestAsync(ValidUserCode, new ConsentResponse { ScopesValuesConsented = ["openid", "api1"] });

        Assert.True(result.IsError);
        Assert.False((await _devices.FindByUserCodeAsync(ValidUserCode)).IsAuthorized);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Consenting_to_requested_scopes_succeeds()
    {
        await StoreDeviceCodeAsync();

        var result = await _subject.HandleRequestAsync(ValidUserCode, new ConsentResponse { ScopesValuesConsented = ["openid"] });

        Assert.False(result.IsError);
        Assert.True((await _devices.FindByUserCodeAsync(ValidUserCode)).IsAuthorized);
    }
}
