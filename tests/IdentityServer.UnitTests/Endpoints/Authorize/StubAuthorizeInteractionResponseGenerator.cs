namespace IdentityServer.UnitTests.Endpoints.Authorize;

internal class StubAuthorizeInteractionResponseGenerator : IAuthorizeInteractionResponseGenerator
{
    internal InteractionResponse Response { get; set; } = new InteractionResponse();

    public Task<InteractionResponse> ProcessInteractionAsync(ValidatedAuthorizeRequest request, ConsentResponse consent = null)
    {
        return Task.FromResult(Response);
    }
}
