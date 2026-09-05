namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_ProtocolValidation_Invalid
{
    private const string Category = "AuthorizeRequest Protocol Validation";

    [Fact]
    [Trait("Category", Category)]
    public Task Null_ParameterAsync()
    {
        var validator = Factory.CreateAuthorizeRequestValidator();

        var act = () => validator.ValidateAsync(null);

        return Assert.ThrowsAnyAsync<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Empty_Parameters()
    {
        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(new NameValueCollection());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_ClientId()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/callback");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_Scope()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_RedirectUri()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Malformed_RedirectUri()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "malformed");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Malformed_RedirectUri_Triple_Slash()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "client");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https:///attacker.com");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Missing_ResponseType()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnsupportedResponseType, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_ResponseType()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, "unknown");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnsupportedResponseType, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task CodeToken_ResponseType_should_require_a_client_opt_in()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_ResponseMode_For_CodeIdToken_ResponseType()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdToken);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseMode, OidcConstants.ResponseModes.Query);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task CodeIdTokenToken_ResponseType_should_require_a_client_opt_in()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.CodeIdTokenToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Malformed_MaxAge()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.MaxAge, "malformed");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Negative_MaxAge()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.MaxAge, "-1");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IdTokenCodeToken_ResponseType_should_require_a_client_opt_in()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "hybridclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, "id_token code token");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task prompt_none_and_other_values_should_fail()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseMode, OidcConstants.ResponseModes.Fragment);
        parameters.Add(OidcConstants.AuthorizeRequest.Prompt, "none login");

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, result.Error);
    }
}
