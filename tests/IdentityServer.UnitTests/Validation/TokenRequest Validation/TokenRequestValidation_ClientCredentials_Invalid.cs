namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_ClientCredentials_Invalid
{
    private const string Category = "TokenRequest Validation - ClientCredentials - Invalid";

    private IClientStore _clients = Factory.CreateClientStore();

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_GrantType_For_Client()
    {
        var client = await _clients.FindEnabledClientByIdAsync("roclient");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_should_succeed_even_with_allowed_identity_scopes_because_they_are_filtered_out()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
        {
            { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials }
        };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
        Assert.Single(result.ValidatedRequest.ValidatedResources.Resources.ApiResources);
        Assert.Equal("api", result.ValidatedRequest.ValidatedResources.Resources.ApiResources.First().Name);

        Assert.Equal(2, result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Count);
        Assert.Equivalent(new[] { "resource", "resource2" }, result.ValidatedRequest.ValidatedResources.Resources.ApiScopes.Select(x => x.Name), strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_Scope()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "unknown");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_Scope_Multiple()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource unknown");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Restricted_Scope()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client_restricted");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource2");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Restricted_Scope_Multiple()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client_restricted");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource resource2");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Identity_scope_is_not_allowed_for_client_credentials_when_specified_explicitly()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
        {
            { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials },
            { OidcConstants.TokenRequest.Scope, "openid" }
        };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Resource_and_Refresh_Token()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource offline_access");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }
}
