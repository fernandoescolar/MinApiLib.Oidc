namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_General_Invalid
{
    private const string Category = "TokenRequest Validation - General - Invalid";

    private IClientStore _clients = new InMemoryClientStore(TestClients.Get());
    private ClaimsPrincipal _subject = new IdentityServerUser("bob").CreatePrincipal();

    [Fact]
    [Trait("Category", Category)]
    public Task Parameters_Null()
    {
        var validator = Factory.CreateTokenRequestValidator();

        var act = () => validator.ValidateRequestAsync(null, null);

        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Client_NullAsync()
    {
        var validator = Factory.CreateTokenRequestValidator();

        var parameters = new NameValueCollection
            {
                { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode },
                { OidcConstants.TokenRequest.Code, "valid" },
                { OidcConstants.TokenRequest.RedirectUri, "https://server/cb" }
            };

        var act = () => validator.ValidateRequestAsync(parameters, null);

        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_Grant_Type()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var store = Factory.CreateAuthorizationCodeStore();

        var code = new AuthorizationCode
        {
            CreationTime = DateTime.UtcNow,
            ClientId = client.ClientId,
            Lifetime = client.AuthorizationCodeLifetime,
            IsOpenId = true,
            RedirectUri = "https://server/cb",
            Subject = _subject
        };

        var handle = await store.StoreAuthorizationCodeAsync(code);

        var validator = Factory.CreateTokenRequestValidator(
            authorizationCodeStore: store);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, "unknown");
        parameters.Add(OidcConstants.TokenRequest.Code, handle);
        parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.UnsupportedGrantType, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Protocol_Type()
    {
        var client = await _clients.FindEnabledClientByIdAsync("client.cred.wsfed");
        var codeStore = Factory.CreateAuthorizationCodeStore();

        var validator = Factory.CreateTokenRequestValidator(
            authorizationCodeStore: codeStore);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.GrantType, "client_credentials");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_Grant_Type()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var store = Factory.CreateAuthorizationCodeStore();

        var code = new AuthorizationCode
        {
            CreationTime = DateTime.UtcNow,
            ClientId = client.ClientId,
            Lifetime = client.AuthorizationCodeLifetime,
            IsOpenId = true,
            RedirectUri = "https://server/cb",
            Subject = _subject
        };

        var handle = await store.StoreAuthorizationCodeAsync(code);

        var validator = Factory.CreateTokenRequestValidator(
            authorizationCodeStore: store);

        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.TokenRequest.Code, handle);
        parameters.Add(OidcConstants.TokenRequest.RedirectUri, "https://server/cb");

        var result = await validator.ValidateRequestAsync(parameters, client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.UnsupportedGrantType, result.Error);
    }
}
