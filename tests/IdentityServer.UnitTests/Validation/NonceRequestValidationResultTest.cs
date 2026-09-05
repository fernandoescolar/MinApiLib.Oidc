namespace IdentityServer.UnitTests.Validation;

public class NonceRequestValidatorTest
{
    private const string Category = "UserInfo Request Validation Tests";
    private IClientStore _clients = new InMemoryClientStore(TestClients.Get());

    [Fact]
    [Trait("Category", Category)]
    public async Task token_parameter_is_missing()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>([new Claim("sub", "123")])
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
        };

        var parameters = new NameValueCollection();

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task token_is_invalid()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = true,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>([new Claim("sub", "123")])
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
        };

        var parameters = new NameValueCollection { { "token", "token" } };

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_is_invalid()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>([new Claim("sub", "123")])
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("codeclient"),
        };

        var parameters = new NameValueCollection { { "token", "token" } };

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task token_without_sub_fail()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>()
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
        };

        var parameters = new NameValueCollection { { "token", "token" } };

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }


    [Fact]
    [Trait("Category", Category)]
    public async Task profile_inactive_fail()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>([new Claim("sub", "123")])
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
        };

        var parameters = new NameValueCollection { { "token", "token" } };

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: false),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ok()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
            Claims = new List<Claim>([new Claim("sub", "123")])
        };

        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = await _clients.FindEnabledClientByIdAsync("nonce_pattern"),
        };

        var parameters = new NameValueCollection { { "token", "token" } };

        var validator = new NonceRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            new MockUserSession(),
            TestLogger.Create<NonceRequestValidator>());

        var result = await validator.ValidateRequestAsync(parameters, clientValidationResult);

        Assert.False(result.IsError);
    }
}
