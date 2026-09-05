namespace IdentityServer.UnitTests.Validation;

public class IntrospectionRequestValidatorTests
{
    private const string Category = "Introspection request validation";

    private IntrospectionRequestValidator _subject;
    private IReferenceTokenStore _referenceTokenStore;

    public IntrospectionRequestValidatorTests()
    {
        _referenceTokenStore = Factory.CreateReferenceTokenStore();
        var tokenValidator = Factory.CreateTokenValidator(_referenceTokenStore);

        _subject = new IntrospectionRequestValidator(tokenValidator, TestLogger.Create<IntrospectionRequestValidator>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_token_should_successfully_validate()
    {
        var token = new Token
        {
            CreationTime = DateTime.UtcNow,
            Issuer = "http://op",
            ClientId = "codeclient",
            Lifetime = 1000,
            Claims =
                {
                    new System.Security.Claims.Claim("scope", "a"),
                    new System.Security.Claims.Claim("scope", "b")
                }
        };
        var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token);

        var param = new NameValueCollection()
            {
                { "token", handle}
            };

        var result = await _subject.ValidateAsync(param, null);

        Assert.False(result.IsError);
        Assert.True(result.IsActive);
        Assert.Equal(5, result.Claims.Count());
        Assert.Equal(handle, result.Token);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_token_should_error()
    {
        var param = new NameValueCollection();

        var result = await _subject.ValidateAsync(param, null);

        Assert.True(result.IsError);
        Assert.Equal("missing_token", result.Error);
        Assert.False(result.IsActive);
        Assert.Null(result.Claims);
        Assert.Null(result.Token);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_token_should_return_inactive()
    {
        var param = new NameValueCollection()
            {
                { "token", "invalid" }
            };

        var result = await _subject.ValidateAsync(param, null);

        Assert.False(result.IsError);
        Assert.False(result.IsActive);
        Assert.Null(result.Claims);
        Assert.Equal("invalid", result.Token);
    }
}
