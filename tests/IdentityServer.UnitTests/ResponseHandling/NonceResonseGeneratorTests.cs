namespace IdentityServer.UnitTests.ResponseHandling;

public class NonceResonseGeneratorTests
{
    private readonly NonceResponseGenerator _target;
    private readonly DefaultAuthorizationNonceStore _nonceStore;
    private readonly StubClock _clock = new();
    private readonly InMemoryPersistedGrantStore _store = new();
    private readonly StubHandleGenerationService _stubHandleGenerationService = new();
    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(new Claim[] { new("sub", "sub") }));

    public NonceResonseGeneratorTests()
    {
        _nonceStore = new(_store,
                 new PersistentGrantSerializer(),
                 _stubHandleGenerationService,
                 TestLogger.Create<DefaultAuthorizationNonceStore>());

        _target = new NonceResponseGenerator(_clock, _nonceStore, TestLogger.Create<NonceResponseGenerator>());
    }

    [Fact]
    public async Task ProcessAsync_should_create_nonce()
    {
        var result = new NonceRequestValidationResult
        {
            SessionId = "session",
            TokenValidationResult = new TokenValidationResult
            {
                Client = new Client
                {
                    ClientId = "client",
                    AuthorizationNonceLifetime = 100
                }
            },
            Subject = _user
        };

        var response = await _target.ProcessAsync(result);
        Assert.NotNull(response);

        var nonce = await _nonceStore.GetAuthorizationNonceAsync(response.Nonce);
        Assert.NotNull(nonce);
        Assert.Equal(result.SessionId, nonce.SessionId);
        Assert.Equal(result.TokenValidationResult.Client.ClientId, nonce.ClientId);
        Assert.Equal(result.TokenValidationResult.Client.AuthorizationNonceLifetime, nonce.Lifetime);
        Assert.Equal(_user.GetSubjectId(), nonce.Subject.GetSubjectId());
    }
}
