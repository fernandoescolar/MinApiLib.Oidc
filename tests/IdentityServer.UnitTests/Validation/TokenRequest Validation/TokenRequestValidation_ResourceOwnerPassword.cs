namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_ResourceOwnerPassword
{
    private const string Category = "TokenRequest Validation - ResourceOwnerPassword";

    private static Client CreateClient(bool allowPasswordFlow = true) => new()
    {
        ClientId = "ropc",
        ClientSecrets = { new Secret("secret".Sha256()) },
        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
        AllowResourceOwnerPasswordFlow = allowPasswordFlow,
        AllowedScopes = { "resource" }
    };

    private static NameValueCollection Parameters(string userName = "bob", string password = "bob") => new()
    {
        { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.Password },
        { OidcConstants.TokenRequest.UserName, userName },
        { OidcConstants.TokenRequest.Password, password },
        { OidcConstants.TokenRequest.Scope, "resource" }
    };

    private sealed class FixedValidator(GrantValidationResult result) : IResourceOwnerPasswordValidator
    {
        public ResourceOwnerPasswordValidationContext Context { get; private set; }

        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            Context = context;
            context.Result = result;
            return Task.CompletedTask;
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_credentials_set_the_subject_and_user_name()
    {
        var resourceOwner = new FixedValidator(new GrantValidationResult("bob", OidcConstants.AuthenticationMethods.Password));
        var validator = Factory.CreateTokenRequestValidator(resourceOwnerValidator: resourceOwner);

        var result = await validator.ValidateRequestAsync(Parameters(), CreateClient().ToValidationResult());

        Assert.False(result.IsError, result.Error);
        Assert.Equal("bob", result.ValidatedRequest.Subject.GetSubjectId());
        Assert.Equal("bob", result.ValidatedRequest.UserName);
        Assert.Equal("bob", resourceOwner.Context.Password);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Client_without_AllowResourceOwnerPasswordFlow_is_unauthorized()
    {
        var resourceOwner = new FixedValidator(new GrantValidationResult("bob", OidcConstants.AuthenticationMethods.Password));
        var validator = Factory.CreateTokenRequestValidator(resourceOwnerValidator: resourceOwner);

        var result = await validator.ValidateRequestAsync(Parameters(), CreateClient(allowPasswordFlow: false).ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.UnauthorizedClient, result.Error);
        Assert.Null(resourceOwner.Context);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Client_without_the_password_grant_is_unauthorized()
    {
        var client = CreateClient();
        client.AllowedGrantTypes = GrantTypes.ClientCredentials;
        var validator = Factory.CreateTokenRequestValidator();

        var result = await validator.ValidateRequestAsync(Parameters(), client.ToValidationResult());

        Assert.Equal(OidcConstants.TokenErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Without_a_registered_validator_the_grant_is_not_supported()
    {
        var validator = Factory.CreateTokenRequestValidator();

        var result = await validator.ValidateRequestAsync(Parameters(), CreateClient().ToValidationResult());

        Assert.Equal(OidcConstants.TokenErrors.UnsupportedGrantType, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Rejected_credentials_return_invalid_grant()
    {
        var validator = Factory.CreateTokenRequestValidator(resourceOwnerValidator: new FixedValidator(new GrantValidationResult(TokenRequestErrors.InvalidGrant)));

        var result = await validator.ValidateRequestAsync(Parameters(), CreateClient().ToValidationResult());

        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, result.Error);
        Assert.Equal("invalid_username_or_password", result.ErrorDescription);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("", "bob")]
    [InlineData("a-user-name-that-is-far-too-long-a-user-name-that-is-far-too-long-a-user-name-that-is-far-too-long-xx", "bob")]
    public async Task Missing_or_too_long_user_name_is_rejected(string userName, string password)
    {
        var resourceOwner = new FixedValidator(new GrantValidationResult("bob", OidcConstants.AuthenticationMethods.Password));
        var validator = Factory.CreateTokenRequestValidator(resourceOwnerValidator: resourceOwner);

        var result = await validator.ValidateRequestAsync(Parameters(userName, password), CreateClient().ToValidationResult());

        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, result.Error);
        Assert.Null(resourceOwner.Context);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Inactive_user_is_rejected()
    {
        var validator = Factory.CreateTokenRequestValidator(
            profile: new TestProfileService(shouldBeActive: false),
            resourceOwnerValidator: new FixedValidator(new GrantValidationResult("bob", OidcConstants.AuthenticationMethods.Password)));

        var result = await validator.ValidateRequestAsync(Parameters(), CreateClient().ToValidationResult());

        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, result.Error);
    }
}
