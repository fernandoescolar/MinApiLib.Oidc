namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_DeviceCode_Invalid
{
    private const string Category = "TokenRequest Validation - DeviceCode - Invalid";

    private readonly IClientStore _clients = Factory.CreateClientStore();

    private readonly DeviceCode deviceCode = new DeviceCode
    {
        ClientId = "device_flow",
        IsAuthorized = true,
        Subject = new IdentityServerUser("bob").CreatePrincipal(),
        IsOpenId = true,
        Lifetime = 300,
        CreationTime = DateTime.UtcNow,
        AuthorizedScopes = new[] { "openid", "profile", "resource" }
    };

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_DeviceCode()
    {
        var client = await _clients.FindClientByIdAsync("device_flow");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
            {
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.DeviceCode}
            };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task DeviceCode_Too_Long()
    {
        var client = await _clients.FindClientByIdAsync("device_flow");

        var longCode = "x".Repeat(new IdentityServerOptions().InputLengthRestrictions.AuthorizationCode + 1);

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
            {
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.DeviceCode},
                {"device_code", longCode}
            };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Grant_For_Client()
    {
        var client = await _clients.FindClientByIdAsync("codeclient");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
            {
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.DeviceCode},
                {"device_code", Guid.NewGuid().ToString()}
            };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task DeviceCodeValidator_Failure()
    {
        var client = await _clients.FindClientByIdAsync("device_flow");

        var validator = Factory.CreateTokenRequestValidator(deviceCodeValidator: new TestDeviceCodeValidator(true));

        var parameters = new NameValueCollection
            {
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.DeviceCode},
                {"device_code", Guid.NewGuid().ToString()}
            };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
        Assert.True(result.IsError);
        Assert.NotNull(result.Error);
    }
}
