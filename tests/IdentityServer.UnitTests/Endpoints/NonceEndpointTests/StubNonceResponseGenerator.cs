namespace IdentityServer.UnitTests.Endpoints.NonceEndpointTests;

internal class StubNonceResponseGenerator : INonceResponseGenerator
{
    private readonly NonceResponse _response;

    public StubNonceResponseGenerator(NonceResponse response)
    {
        _response = response;
    }

    public Task<NonceResponse> ProcessAsync(NonceRequestValidationResult validationResult)
    {
        return Task.FromResult(_response);
    }
}
