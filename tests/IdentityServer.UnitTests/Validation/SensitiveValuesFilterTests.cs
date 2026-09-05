namespace IdentityServer.UnitTests.Validation;

public class SensitiveValuesFilterTests
{
    private const string Category = "Sensitive values filter";
    private const string Redacted = "***REDACTED***";

    [Fact]
    [Trait("Category", Category)]
    public void Authorize_request_log_redacts_login_hint_token()
    {
        var request = new ValidatedAuthorizeRequest
        {
            Raw = new NameValueCollection
            {
                { IdentityServerConstants.NonceAuthorization.NonceTokenParameter, "nonce-handle" },
                { OidcConstants.AuthorizeRequest.ClientId, "client" }
            }
        };

        var log = new IdentityServer.Logging.Models.AuthorizeRequestValidationLog(request, new LoggingOptions().AuthorizeRequestSensitiveValuesFilter);

        Assert.Equal(Redacted, log.Raw[IdentityServerConstants.NonceAuthorization.NonceTokenParameter]);
        Assert.Equal("client", log.Raw[OidcConstants.AuthorizeRequest.ClientId]);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Token_request_log_redacts_code_and_code_verifier()
    {
        var request = new ValidatedTokenRequest
        {
            Raw = new NameValueCollection
            {
                { OidcConstants.TokenRequest.Code, "authorization-code" },
                { OidcConstants.TokenRequest.CodeVerifier, "code-verifier" },
                { OidcConstants.TokenRequest.GrantType, "authorization_code" }
            }
        };

        var log = new IdentityServer.Logging.Models.TokenRequestValidationLog(request, new LoggingOptions().TokenRequestSensitiveValuesFilter);

        Assert.Equal(Redacted, log.Raw[OidcConstants.TokenRequest.Code]);
        Assert.Equal(Redacted, log.Raw[OidcConstants.TokenRequest.CodeVerifier]);
        Assert.Equal("authorization_code", log.Raw[OidcConstants.TokenRequest.GrantType]);
    }
}
