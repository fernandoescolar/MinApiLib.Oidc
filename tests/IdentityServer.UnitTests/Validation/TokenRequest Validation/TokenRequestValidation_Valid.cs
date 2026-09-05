namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_Valid
{
    private const string Category = "TokenRequest Validation - General - Valid";

    private IClientStore _clients = Factory.CreateClientStore();

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_code_request_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var grants = Factory.CreateAuthorizationCodeStore();

        var code = new AuthorizationCode
        {
            CreationTime = DateTime.UtcNow,
            Subject = new IdentityServerUser("123").CreatePrincipal(),
            ClientId = client.ClientId,
            Lifetime = client.AuthorizationCodeLifetime,
            RedirectUri = "https://server/cb",
            RequestedScopes = new List<string>
            {
                "openid"
            }
        };

        var handle = await grants.StoreAuthorizationCodeAsync(code);

        var validator = Factory.CreateTokenRequestValidator(
            authorizationCodeStore: grants);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
        parameters.Add(OidcConstants.TokenRequest.Code, handle);
        parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_code_request_with_refresh_token_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var grants = Factory.CreateAuthorizationCodeStore();

        var code = new AuthorizationCode
        {
            CreationTime = DateTime.UtcNow,
            ClientId = client.ClientId,
            Lifetime = client.AuthorizationCodeLifetime,
            Subject = new IdentityServerUser("123").CreatePrincipal(),
            RedirectUri = "https://server/cb",
            RequestedScopes = new List<string>
            {
                "openid",
                "offline_access"
            }
        };

        var handle = await grants.StoreAuthorizationCodeAsync(code);

        var validator = Factory.CreateTokenRequestValidator(
            authorizationCodeStore: grants);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode);
        parameters.Add(OidcConstants.TokenRequest.Code, handle);
        parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_client_credentials_request_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_client_credentials_request_with_default_scopes_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client_restricted");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }


    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_client_credentials_request_restricted_client_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client_restricted");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.ClientCredentials);
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_extension_grant_request_should_succeed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("customgrantclient");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, "custom_grant");
        parameters.Add(OidcConstants.TokenRequest.Scope, "resource");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_refresh_token_request_should_succeed()
    {
        var subjectClaim = new Claim(JwtClaimTypes.Subject, "foo");

        var refreshToken = new RefreshToken
        {
            AccessToken = new Token("access_token")
            {
                Claims = new List<Claim> { subjectClaim, new Claim(JwtClaimTypes.Scope, "resource"), new Claim(JwtClaimTypes.Scope, "offline_access") },
                ClientId = "roclient"
            },
            Lifetime = 600,
            CreationTime = DateTime.UtcNow
        };

        var grants = Factory.CreateRefreshTokenStore();
        var handle = await grants.StoreRefreshTokenAsync(refreshToken);

        var client = await _clients.FindEnabledClientByIdAsync("roclient");

        var validator = Factory.CreateTokenRequestValidator(
            refreshTokenStore: grants);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
        parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_refresh_token_request_using_restricted_client_should_succeed()
    {
        var subjectClaim = new Claim(JwtClaimTypes.Subject, "foo");

        var refreshToken = new RefreshToken
        {
            AccessToken = new Token("access_token")
            {
                Claims = new List<Claim> { subjectClaim, new Claim(JwtClaimTypes.Scope, "resource"), new Claim(JwtClaimTypes.Scope, "offline_access") },
                ClientId = "roclient_restricted_refresh"
            },

            Lifetime = 600,
            CreationTime = DateTime.UtcNow
        };

        var grants = Factory.CreateRefreshTokenStore();
        var handle = await grants.StoreRefreshTokenAsync(refreshToken);

        var client = await _clients.FindEnabledClientByIdAsync("roclient_restricted_refresh");

        var validator = Factory.CreateTokenRequestValidator(
            refreshTokenStore: grants);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, "refresh_token");
        parameters.Add(OidcConstants.TokenRequest.RefreshToken, handle);

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_device_code_request_should_succeed()
    {
        var deviceCode = new DeviceCode
        {
            ClientId = "device_flow",
            IsAuthorized = true,
            Subject = new IdentityServerUser("bob").CreatePrincipal(),
            IsOpenId = true,
            Lifetime = 300,
            CreationTime = DateTime.UtcNow,
            AuthorizedScopes = new[] { "openid", "profile", "resource" }
        };

        var client = await _clients.FindClientByIdAsync("device_flow");

        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
        {
            {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.DeviceCode},
            {"device_code", Guid.NewGuid().ToString()}
        };

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());
        Assert.False(result.IsError);
    }
}
