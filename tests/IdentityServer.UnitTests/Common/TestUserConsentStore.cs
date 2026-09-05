namespace IdentityServer.UnitTests.Common;

public class TestUserConsentStore : IUserConsentStore
{
    private DefaultUserConsentStore _userConsentStore;
    private InMemoryPersistedGrantStore _grantStore = new InMemoryPersistedGrantStore();

    public TestUserConsentStore()
    {
        _userConsentStore = new DefaultUserConsentStore(
           _grantStore,
           new PersistentGrantSerializer(),
            new DefaultHandleGenerationService(),
           TestLogger.Create<DefaultUserConsentStore>());
    }

    public Task StoreUserConsentAsync(Consent consent)
    {
        return _userConsentStore.StoreUserConsentAsync(consent);
    }

    public Task<Consent> GetUserConsentAsync(string subjectId, string clientId)
    {
        return _userConsentStore.GetUserConsentAsync(subjectId, clientId);
    }

    public Task RemoveUserConsentAsync(string subjectId, string clientId)
    {
        return _userConsentStore.RemoveUserConsentAsync(subjectId, clientId);
    }
}
