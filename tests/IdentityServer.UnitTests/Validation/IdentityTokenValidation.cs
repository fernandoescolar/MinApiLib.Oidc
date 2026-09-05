namespace IdentityServer.UnitTests.Validation;

public class IdentityTokenValidation
{
    private const string Category = "Identity token validation";

    static IdentityTokenValidation()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_IdentityToken_DefaultKeyType()
    {
        var creator = Factory.CreateDefaultTokenCreator();
        var token = TokenFactory.CreateIdentityToken("roclient", "valid");
        var jwt = await creator.CreateTokenAsync(token);

        var validator = Factory.CreateTokenValidator();
        var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_IdentityToken_DefaultKeyType_no_ClientId_supplied()
    {
        var creator = Factory.CreateDefaultTokenCreator();
        var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");
        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_IdentityToken_no_ClientId_supplied()
    {
        var creator = Factory.CreateDefaultTokenCreator();
        var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateIdentityTokenAsync(jwt);
        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IdentityToken_InvalidClientId()
    {
        var creator = Factory.CreateDefaultTokenCreator();
        var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityToken("roclient", "valid"));
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateIdentityTokenAsync(jwt, "invalid");
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IdentityToken_Too_Long()
    {
        var creator = Factory.CreateDefaultTokenCreator();
        var jwt = await creator.CreateTokenAsync(TokenFactory.CreateIdentityTokenLong("roclient", "valid", 1000));
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateIdentityTokenAsync(jwt, "roclient");
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }
}
