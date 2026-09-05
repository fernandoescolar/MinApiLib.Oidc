namespace IdentityServer.UnitTests.Common;

public class MockConsentService : IConsentService
{
    public bool RequiresConsentResult { get; set; }

    public Task<bool> RequiresConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
    {
        return Task.FromResult(RequiresConsentResult);
    }

    public ClaimsPrincipal ConsentSubject { get; set; }
    public Client ConsentClient { get; set; }
    public IEnumerable<string> ConsentScopes { get; set; }

    public Task UpdateConsentAsync(ClaimsPrincipal subject, Client client, IEnumerable<ParsedScopeValue> parsedScopes)
    {
        ConsentSubject = subject;
        ConsentClient = client;
        ConsentScopes = parsedScopes?.Select(x => x.RawValue);

        return Task.CompletedTask;
    }
}
