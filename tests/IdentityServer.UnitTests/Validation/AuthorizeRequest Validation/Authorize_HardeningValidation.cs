namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_HardeningValidation
{
    private const string Category = "AuthorizeRequest Validation - Hardening";

    private static NameValueCollection CreateParameters(string redirectUri = "https://server/cb")
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, redirectUri);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        return parameters;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task State_longer_than_limit_should_fail()
    {
        var parameters = CreateParameters();
        parameters.Add(OidcConstants.AuthorizeRequest.State, new string('x', 4001));

        var result = await Factory.CreateAuthorizeRequestValidator().ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal("Invalid state", result.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task State_at_limit_should_succeed()
    {
        var parameters = CreateParameters();
        parameters.Add(OidcConstants.AuthorizeRequest.State, new string('x', 4000));

        var result = await Factory.CreateAuthorizeRequestValidator().ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Redirect_uri_with_fragment_should_fail_even_when_registered()
    {
        var client = new Client
        {
            ClientId = "codeclient",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = { "openid" },
            RequirePkce = false,
            RedirectUris = { "https://server/cb#fragment" }
        };
        var validator = Factory.CreateAuthorizeRequestValidator(clients: new InMemoryClientStore([client]));

        var result = await validator.ValidateAsync(CreateParameters("https://server/cb#fragment"));

        Assert.True(result.IsError);
        Assert.Equal("Invalid redirect_uri", result.ErrorDescription);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("create")]
    [InlineData("login unknown")]
    public async Task Unsupported_prompt_should_fail(string prompt)
    {
        var parameters = CreateParameters();
        parameters.Add(OidcConstants.AuthorizeRequest.Prompt, prompt);

        var result = await Factory.CreateAuthorizeRequestValidator().ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
        Assert.Equal("Invalid prompt", result.ErrorDescription);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("login")]
    [InlineData("consent select_account")]
    public async Task Supported_prompt_should_succeed(string prompt)
    {
        var parameters = CreateParameters();
        parameters.Add(OidcConstants.AuthorizeRequest.Prompt, prompt);

        var result = await Factory.CreateAuthorizeRequestValidator().ValidateAsync(parameters);

        Assert.False(result.IsError);
    }
}
