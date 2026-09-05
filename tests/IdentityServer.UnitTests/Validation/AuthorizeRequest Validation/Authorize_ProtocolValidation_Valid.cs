namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_ProtocolValidation_Valid
{
    private const string Category = "AuthorizeRequest Protocol Validation - Valid";

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
    public async Task Valid_ResponseMode_For_Code_ResponseType()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseMode, OidcConstants.ResponseModes.Fragment);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task anonymous_user_should_produce_session_state_value()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseMode, OidcConstants.ResponseModes.Fragment);
        parameters.Add(OidcConstants.AuthorizeRequest.Prompt, OidcConstants.PromptModes.None);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.NotNull(result.ValidatedRequest.SessionId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task multiple_prompt_values_should_be_accepted()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "codeclient");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://server/cb");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code);
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseMode, OidcConstants.ResponseModes.Fragment);
        parameters.Add(OidcConstants.AuthorizeRequest.Prompt, OidcConstants.PromptModes.Consent.ToString() + " " + OidcConstants.PromptModes.Login.ToString());

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.Equal(2, result.ValidatedRequest.PromptModes.Count());
        Assert.Contains(OidcConstants.PromptModes.Login, result.ValidatedRequest.PromptModes);
        Assert.Contains(OidcConstants.PromptModes.Consent, result.ValidatedRequest.PromptModes);
    }
}
