namespace IdentityServer.UnitTests.Endpoints.NonceEndpointTests;

internal class StubClientSecretValidator : IClientSecretValidator
{
    private readonly ClientSecretValidationResult _result;

    public StubClientSecretValidator(ClientSecretValidationResult result)
    {
        _result = result;
    }
    public Task<ClientSecretValidationResult> ValidateAsync(HttpContext context)
    {
        return Task.FromResult(_result);
    }
}
