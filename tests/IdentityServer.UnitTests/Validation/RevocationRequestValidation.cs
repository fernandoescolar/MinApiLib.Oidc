namespace IdentityServer.UnitTests.Validation;

public class RevocationRequestValidation
{
    private const string Category = "Revocation Request Validation Tests";

    private ITokenRevocationRequestValidator _validator;
    private Client _client;

    public RevocationRequestValidation()
    {
        _validator = new TokenRevocationRequestValidator(TestLogger.Create<TokenRevocationRequestValidator>());
        _client = new Client
        {
            ClientName = "Code Client",
            Enabled = true,
            ClientId = "codeclient",
            ClientSecrets = new List<Secret>
                {
                    new Secret("secret".Sha256())
                },

            AllowedGrantTypes = GrantTypes.Code,

            RequireConsent = false,

            RedirectUris = new List<string>
                {
                    "https://server/cb"
                },

            AuthorizationCodeLifetime = 60
        };
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Empty_Parameters()
    {
        var parameters = new NameValueCollection();

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_Token_Valid_Hint()
    {
        var parameters = new NameValueCollection
            {
                { "token_type_hint", "access_token" }
            };

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Token_And_AccessTokenHint()
    {
        var parameters = new NameValueCollection
            {
                { "token", "foo" },
                { "token_type_hint", "access_token" }
            };

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.False(result.IsError);
        Assert.Equal("foo", result.Token);
        Assert.Equal("access_token", result.TokenTypeHint);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Token_and_RefreshTokenHint()
    {
        var parameters = new NameValueCollection
            {
                { "token", "foo" },
                { "token_type_hint", "refresh_token" }
            };

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.False(result.IsError);
        Assert.Equal("foo", result.Token);
        Assert.Equal("refresh_token", result.TokenTypeHint);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Token_And_Missing_Hint()
    {
        var parameters = new NameValueCollection
            {
                { "token", "foo" }
            };

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.False(result.IsError);
        Assert.Equal("foo", result.Token);
        Assert.Null(result.TokenTypeHint);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Token_And_Invalid_Hint()
    {
        var parameters = new NameValueCollection
            {
                { "token", "foo" },
                { "token_type_hint", "invalid" }
            };

        var result = await _validator.ValidateRequestAsync(parameters, _client);

        Assert.True(result.IsError);
        Assert.Equal(Constants.RevocationErrors.UnsupportedTokenType, result.Error);
    }
}
