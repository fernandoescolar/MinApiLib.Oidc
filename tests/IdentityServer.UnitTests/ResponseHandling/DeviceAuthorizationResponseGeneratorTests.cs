namespace IdentityServer.UnitTests.ResponseHandling;

public class DeviceAuthorizationResponseGeneratorTests
{
    private readonly List<IdentityResource> identityResources = new List<IdentityResource> { new IdentityResources.OpenId(), new IdentityResources.Profile() };
    private readonly List<ApiResource> apiResources = new List<ApiResource> { new ApiResource("resource") { Scopes = { "api1" } } };
    private readonly List<ApiScope> scopes = new List<ApiScope> { new ApiScope("api1") };

    private readonly FakeUserCodeGenerator fakeUserCodeGenerator = new FakeUserCodeGenerator();
    private readonly IDeviceFlowCodeService deviceFlowCodeService = new DefaultDeviceFlowCodeService(new InMemoryDeviceFlowStore(), new StubHandleGenerationService());
    private readonly IdentityServerOptions options = new IdentityServerOptions();
    private readonly StubClock clock = new StubClock();

    private readonly DeviceAuthorizationResponseGenerator generator;
    private readonly DeviceAuthorizationRequestValidationResult testResult;
    private const string TestBaseUrl = "http://localhost:5000/";

    public DeviceAuthorizationResponseGeneratorTests()
    {
        testResult = new DeviceAuthorizationRequestValidationResult(new ValidatedDeviceAuthorizationRequest
        {
            Client = new Client { ClientId = Guid.NewGuid().ToString() },
            IsOpenIdRequest = true,
            ValidatedResources = new ResourceValidationResult()
        });

        generator = new DeviceAuthorizationResponseGenerator(
            options,
            new DefaultUserCodeService(new IUserCodeGenerator[] { new NumericUserCodeGenerator(), fakeUserCodeGenerator }),
            deviceFlowCodeService,
            clock,
            new NullLogger<DeviceAuthorizationResponseGenerator>());
    }

    [Fact]
    public Task ProcessAsync_when_valiationresult_null_exect_exceptionAsync()
    {
        var act = () => generator.ProcessAsync(null, TestBaseUrl);
        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    public Task ProcessAsync_when_valiationresult_client_null_exect_exceptionAsync()
    {
        var validationResult = new DeviceAuthorizationRequestValidationResult(new ValidatedDeviceAuthorizationRequest());
        var act = () => generator.ProcessAsync(validationResult, TestBaseUrl);
        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    public Task ProcessAsync_when_baseurl_null_exect_exceptionAsync()
    {
        var act = () => generator.ProcessAsync(testResult, null);
        return Assert.ThrowsAnyAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task ProcessAsync_when_user_code_collision_expect_retry()
    {
        var creationTime = DateTime.UtcNow;
        clock.UtcNowFunc = () => creationTime;

        testResult.ValidatedRequest.Client.UserCodeType = FakeUserCodeGenerator.UserCodeTypeValue;
        await deviceFlowCodeService.StoreDeviceAuthorizationAsync(FakeUserCodeGenerator.TestCollisionUserCode, new DeviceCode());

        var response = await generator.ProcessAsync(testResult, TestBaseUrl);

        Assert.Equal(FakeUserCodeGenerator.TestUniqueUserCode, response.UserCode);
    }

    [Fact]
    public async Task ProcessAsync_when_user_code_collision_retry_limit_reached_expect_error()
    {
        var creationTime = DateTime.UtcNow;
        clock.UtcNowFunc = () => creationTime;

        fakeUserCodeGenerator.RetryLimit = 1;
        testResult.ValidatedRequest.Client.UserCodeType = FakeUserCodeGenerator.UserCodeTypeValue;
        await deviceFlowCodeService.StoreDeviceAuthorizationAsync(FakeUserCodeGenerator.TestCollisionUserCode, new DeviceCode());

        await Assert.ThrowsAsync<InvalidOperationException>(() => generator.ProcessAsync(testResult, TestBaseUrl));
    }

    [Fact]
    public async Task ProcessAsync_when_generated_expect_user_code_stored()
    {
        var creationTime = DateTime.UtcNow;
        clock.UtcNowFunc = () => creationTime;

        testResult.ValidatedRequest.RequestedScopes = new List<string> { "openid", "api1" };
        testResult.ValidatedRequest.ValidatedResources = new ResourceValidationResult(new Resources(
            identityResources.Where(x => x.Name == "openid"),
            apiResources.Where(x => x.Name == "resource"),
            scopes.Where(x => x.Name == "api1")));

        var response = await generator.ProcessAsync(testResult, TestBaseUrl);

        Assert.False(string.IsNullOrWhiteSpace(response.UserCode));

        var userCode = await deviceFlowCodeService.FindByUserCodeAsync(response.UserCode);
        Assert.NotNull(userCode);
        Assert.Equal(testResult.ValidatedRequest.Client.ClientId, userCode.ClientId);
        Assert.Equal(testResult.ValidatedRequest.Client.DeviceCodeLifetime, userCode.Lifetime);
        Assert.Equal(creationTime, userCode.CreationTime);
        Assert.Null(userCode.Subject);
        Assert.Null(userCode.AuthorizedScopes);

        Assert.All(testResult.ValidatedRequest.RequestedScopes, expected => Assert.Contains(expected, userCode.RequestedScopes));
    }

    [Fact]
    public async Task ProcessAsync_when_generated_expect_device_code_stored()
    {
        var creationTime = DateTime.UtcNow;
        clock.UtcNowFunc = () => creationTime;

        var response = await generator.ProcessAsync(testResult, TestBaseUrl);

        Assert.False(string.IsNullOrWhiteSpace(response.DeviceCode));
        Assert.Equal(options.DeviceFlow.Interval, response.Interval);

        var deviceCode = await deviceFlowCodeService.FindByDeviceCodeAsync(response.DeviceCode);
        Assert.NotNull(deviceCode);
        Assert.Equal(testResult.ValidatedRequest.Client.ClientId, deviceCode.ClientId);
        Assert.Equal(testResult.ValidatedRequest.IsOpenIdRequest, deviceCode.IsOpenId);
        Assert.Equal(testResult.ValidatedRequest.Client.DeviceCodeLifetime, deviceCode.Lifetime);
        Assert.Equal(creationTime, deviceCode.CreationTime);
        Assert.Null(deviceCode.Subject);
        Assert.Null(deviceCode.AuthorizedScopes);

        Assert.Equal(deviceCode.Lifetime, response.DeviceCodeLifetime);
    }

    [Fact]
    public async Task ProcessAsync_when_DeviceVerificationUrl_is_relative_uri_expect_correct_VerificationUris()
    {
        const string baseUrl = "http://localhost:5000/";
        options.UserInteraction.DeviceVerificationUrl = "/device";
        options.UserInteraction.DeviceVerificationUserCodeParameter = "userCode";

        var response = await generator.ProcessAsync(testResult, baseUrl);

        Assert.Equal("http://localhost:5000/device", response.VerificationUri);
        Assert.StartsWith("http://localhost:5000/device?userCode=", response.VerificationUriComplete);
    }

    [Fact]
    public async Task ProcessAsync_when_DeviceVerificationUrl_is_absolute_uri_expect_correct_VerificationUris()
    {
        const string baseUrl = "http://localhost:5000/";
        options.UserInteraction.DeviceVerificationUrl = "http://short/device";
        options.UserInteraction.DeviceVerificationUserCodeParameter = "userCode";

        var response = await generator.ProcessAsync(testResult, baseUrl);

        Assert.Equal("http://short/device", response.VerificationUri);
        Assert.StartsWith("http://short/device?userCode=", response.VerificationUriComplete);
    }
}

internal class FakeUserCodeGenerator : IUserCodeGenerator
{
    public const string UserCodeTypeValue = "Collider";
    public const string TestUniqueUserCode = "123";
    public const string TestCollisionUserCode = "321";
    private int tryCount = 0;
    private int retryLimit = 2;

    public string UserCodeType => UserCodeTypeValue;

    public int RetryLimit
    {
        get => retryLimit;
        set => retryLimit = value;
    }

    public Task<string> GenerateAsync()
    {
        if (tryCount == 0)
        {
            tryCount++;
            return Task.FromResult(TestCollisionUserCode);
        }

        tryCount++;
        return Task.FromResult(TestUniqueUserCode);
    }
}
