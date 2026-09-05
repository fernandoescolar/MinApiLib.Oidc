namespace IdentityServer.UnitTests.Endpoints.Authorize;

public class StubAuthorizeRequestValidator : IAuthorizeRequestValidator
{
    public AuthorizeRequestValidationResult Result { get; set; }

    public Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject = null)
    {
        return Task.FromResult(Result);
    }
}
