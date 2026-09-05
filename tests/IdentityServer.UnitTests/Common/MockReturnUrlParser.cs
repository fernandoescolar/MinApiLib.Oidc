namespace IdentityServer.UnitTests.Common;

public class MockReturnUrlParser : ReturnUrlParser
{
    public AuthorizationRequest AuthorizationRequestResult { get; set; }
    public bool IsValidReturnUrlResult { get; set; }

    public MockReturnUrlParser() : base(Enumerable.Empty<IReturnUrlParser>())
    {
    }

    public override Task<AuthorizationRequest> ParseAsync(string returnUrl)
    {
        return Task.FromResult(AuthorizationRequestResult);
    }

    public override bool IsValidReturnUrl(string returnUrl)
    {
        return IsValidReturnUrlResult;
    }
}
