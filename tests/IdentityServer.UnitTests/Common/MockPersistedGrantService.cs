namespace IdentityServer.UnitTests.Common;

public class MockPersistedGrantService : IPersistedGrantService
{
    public IEnumerable<Grant> GetAllGrantsResult { get; set; }
    public bool RemoveAllGrantsWasCalled { get; set; }

    public Task<IEnumerable<Grant>> GetAllGrantsAsync(string subjectId)
    {
        return Task.FromResult(GetAllGrantsResult ?? Enumerable.Empty<Grant>());
    }

    public Task RemoveAllGrantsAsync(string subjectId, string clientId, string sessionId = null)
    {
        RemoveAllGrantsWasCalled = true;
        return Task.CompletedTask;
    }
}
