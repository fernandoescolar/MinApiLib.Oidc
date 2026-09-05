namespace IdentityServer.UnitTests.Endpoints.EndSession;

public class EndSessionCallbackEndpointTests
{
    private const string Category = "End Session Callback Endpoint";

    StubEndSessionRequestValidator _stubEndSessionRequestValidator = new StubEndSessionRequestValidator();
    EndSessionCallbackEndpoint _subject;

    public EndSessionCallbackEndpointTests()
    {
        _subject = new EndSessionCallbackEndpoint(
            _stubEndSessionRequestValidator,
            new LoggerFactory().CreateLogger<EndSessionCallbackEndpoint>());
    }
}
