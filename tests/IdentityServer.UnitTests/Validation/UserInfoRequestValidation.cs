namespace IdentityServer.UnitTests.Validation;

public class UserInfoRequestValidation
{
    private const string Category = "UserInfo Request Validation Tests";
    private IClientStore _clients = new InMemoryClientStore(TestClients.Get());

    [Fact]
    [Trait("Category", Category)]
    public async Task token_without_sub_should_fail()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("codeclient"),
            Claims = new List<Claim>()
        };

        var validator = new UserInfoRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            TestLogger.Create<UserInfoRequestValidator>());

        var result = await validator.ValidateRequestAsync("token");

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task active_user_should_succeed()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("codeclient"),
            Claims = new List<Claim>
                {
                    new Claim("sub", "123")
                },
        };

        var validator = new UserInfoRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: true),
            TestLogger.Create<UserInfoRequestValidator>());

        var result = await validator.ValidateRequestAsync("token");

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task inactive_user_should_fail()
    {
        var tokenResult = new TokenValidationResult
        {
            IsError = false,
            Client = await _clients.FindEnabledClientByIdAsync("codeclient"),
            Claims = new List<Claim>
                {
                    new Claim("sub", "123")
                },
        };

        var validator = new UserInfoRequestValidator(
            new TestTokenValidator(tokenResult),
            new TestProfileService(shouldBeActive: false),
            TestLogger.Create<UserInfoRequestValidator>());

        var result = await validator.ValidateRequestAsync("token");

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }
}
