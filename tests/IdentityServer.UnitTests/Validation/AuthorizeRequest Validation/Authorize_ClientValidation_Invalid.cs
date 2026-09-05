namespace IdentityServer.UnitTests.Validation.AuthorizeRequest_Validation;

public class Authorize_ClientValidation_Invalid
{
    private const string Category = "AuthorizeRequest Client Validation - Invalid";

    private IdentityServerOptions _options = TestIdentityServerOptions.Create();

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Protocol_Client()
    {
        var parameters = new NameValueCollection();
        parameters.Add(OidcConstants.AuthorizeRequest.ClientId, "wsfed");
        parameters.Add(OidcConstants.AuthorizeRequest.Scope, "openid");
        parameters.Add(OidcConstants.AuthorizeRequest.RedirectUri, "https://wsfed/callback");
        parameters.Add(OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.IdToken);

        var validator = Factory.CreateAuthorizeRequestValidator();
        var result = await validator.ValidateAsync(parameters);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, result.Error);
    }
}
