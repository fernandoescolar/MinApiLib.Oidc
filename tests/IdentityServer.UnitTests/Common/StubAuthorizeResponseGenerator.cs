namespace IdentityServer.UnitTests.Common;

internal class StubAuthorizeResponseGenerator : IAuthorizeResponseGenerator
{
    public AuthorizeResponse Response { get; set; } = new AuthorizeResponse();

    public Task<AuthorizeResponse> CreateResponseAsync(ValidatedAuthorizeRequest request)
    {
        return Task.FromResult(Response);
    }
}
