namespace IdentityServer.UnitTests.Validation;

public class DeviceAuthorizationRequestValidation
{
    private const string Category = "Device authorization request validation";

    private readonly NameValueCollection testParameters = new NameValueCollection { { "scope", "resource" } };
    private readonly Client testClient = new Client
    {
        ClientId = "device_flow",
        AllowedGrantTypes = GrantTypes.DeviceFlow,
        AllowedScopes = { "openid", "profile", "resource" },
        AllowOfflineAccess = true
    };

    [Fact]
    [Trait("Category", Category)]
    public Task Null_Parameter()
    {
        var validator = Factory.CreateDeviceAuthorizationRequestValidator();

        var act = () => validator.ValidateAsync(null, null);

        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Protocol_Client()
    {
        testClient.ProtocolType = IdentityServerConstants.ProtocolTypes.WsFederation;

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(testParameters, new ClientSecretValidationResult { Client = testClient });

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Grant_Type()
    {
        testClient.AllowedGrantTypes = GrantTypes.ClientCredentials;

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(testParameters, new ClientSecretValidationResult { Client = testClient });

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unauthorized_Scope()
    {
        var parameters = new NameValueCollection { { "scope", "resource2" } };

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(parameters, new ClientSecretValidationResult { Client = testClient });

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_Scope()
    {
        var parameters = new NameValueCollection { { "scope", Guid.NewGuid().ToString() } };

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(parameters, new ClientSecretValidationResult { Client = testClient });

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_OpenId_Request()
    {
        var parameters = new NameValueCollection { { "scope", "openid" } };

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(parameters, new ClientSecretValidationResult { Client = testClient });

        Assert.False(result.IsError);
        Assert.True(result.ValidatedRequest.IsOpenIdRequest);
        Assert.Contains("openid", result.ValidatedRequest.RequestedScopes);

        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources, x => x.Name == "openid");
        Assert.Empty(result.ValidatedRequest.ValidatedResources.Resources.ApiResources);
        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);

        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources.Any());
        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.ApiResources.Any());
        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Resource_Request()
    {
        var parameters = new NameValueCollection { { "scope", "resource" } };

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(parameters, new ClientSecretValidationResult { Client = testClient });

        Assert.False(result.IsError);
        Assert.False(result.ValidatedRequest.IsOpenIdRequest);
        Assert.Contains("resource", result.ValidatedRequest.RequestedScopes);

        Assert.Empty(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources);
        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.ApiResources, x => x.Name == "api");
        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.ApiScopes, x => x.Name == "resource");
        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);

        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources.Any());
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.ApiResources.Any());
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Any());
        Assert.False(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Mixed_Request()
    {
        var parameters = new NameValueCollection { { "scope", "openid resource offline_access" } };

        var validator = Factory.CreateDeviceAuthorizationRequestValidator();
        var result = await validator.ValidateAsync(parameters, new ClientSecretValidationResult { Client = testClient });

        Assert.False(result.IsError);
        Assert.True(result.ValidatedRequest.IsOpenIdRequest);
        Assert.Contains("openid", result.ValidatedRequest.RequestedScopes);
        Assert.Contains("resource", result.ValidatedRequest.RequestedScopes);
        Assert.Contains("offline_access", result.ValidatedRequest.RequestedScopes);

        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources, x => x.Name == "openid");
        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.ApiResources, x => x.Name == "api");
        Assert.Contains(result.ValidatedRequest.ValidatedResources.Resources.ApiScopes, x => x.Name == "resource");
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);

        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.IdentityResources.Any());
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.ApiResources.Any());
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Any());
        Assert.True(result.ValidatedRequest.ValidatedResources.Resources.OfflineAccess);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_Scopes_Expect_Client_Scopes()
    {
        var validator = Factory.CreateDeviceAuthorizationRequestValidator();

        var result = await validator.ValidateAsync(
            new NameValueCollection(),
            new ClientSecretValidationResult { Client = testClient });

        Assert.False(result.IsError);
        Assert.All(testClient.AllowedScopes, expected => Assert.Contains(expected, result.ValidatedRequest.RequestedScopes));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_Scopes_And_Client_Scopes_Empty()
    {
        testClient.AllowedScopes.Clear();
        var validator = Factory.CreateDeviceAuthorizationRequestValidator();

        var result = await validator.ValidateAsync(
            new NameValueCollection(),
            new ClientSecretValidationResult { Client = testClient });

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidScope, result.Error);
    }
}
