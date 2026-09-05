namespace IdentityServer.UnitTests.Endpoints.NonceEndpointTests;

internal class StubNonceRequestValidator : INonceRequestValidator
{
    private readonly NonceRequestValidationResult _result;

    public StubNonceRequestValidator(NonceRequestValidationResult result)
    {
        _result = result;
    }

    public Task<NonceRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientResult)
    {
        return Task.FromResult(_result);
    }
}
