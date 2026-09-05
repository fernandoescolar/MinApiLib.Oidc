namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_ClientValidation_Valid
{
    private const string Category = "AuthorizeRequest Client Validation - Valid";

    private IdentityServerOptions _options = TestIdentityServerOptions.Create();

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_OpenId_Code_Request()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Resource_Code_Request()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "resource");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Mixed_Code_Request()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid resource");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Mixed_Code_Request_Multiple_Scopes()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid profile resource");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_OpenId_CodeIdToken_Request()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.Nonce, "nonce");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task OpenId_CodeIdTokenToken_Request_should_require_a_client_opt_in()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.Nonce, "nonce");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdTokenToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        // Access tokens from the authorization endpoint need AllowAccessTokensViaBrowser (OAuth 2.1).
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Mixed_CodeIdToken_Request()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid resource");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.Nonce, "nonce");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Mixed_CodeIdTokenToken_Request_should_require_a_client_opt_in()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid resource");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.Nonce, "nonce");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdTokenToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        // Access tokens from the authorization endpoint need AllowAccessTokensViaBrowser (OAuth 2.1).
        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

}
