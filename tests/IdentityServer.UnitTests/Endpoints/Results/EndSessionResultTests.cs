namespace IdentityServer.UnitTests.Endpoints.Results;

public class EndSessionResultTests
{
    private EndSessionResult _subject;

    private EndSessionValidationResult _result = new EndSessionValidationResult();
    private IdentityServerOptions _options = new IdentityServerOptions();
    private MockMessageStore<LogoutMessage> _mockLogoutMessageStore = new MockMessageStore<LogoutMessage>();

    private DefaultHttpContext _context = new DefaultHttpContext();

    public EndSessionResultTests()
    {
        _context.SetIdentityServerOrigin("https://server");
        _context.SetIdentityServerBasePath("/");

        _options.UserInteraction.LogoutUrl = "~/logout";
        _options.UserInteraction.LogoutIdParameter = "logoutId";

        _subject = new EndSessionResult(_result, _options, new StubClock(), _mockLogoutMessageStore);
    }

    [Fact]
    public async Task validated_signout_should_pass_logout_message()
    {
        _result.IsError = false;
        _result.ValidatedRequest = new ValidatedEndSessionRequest
        {
            Client = new Client
            {
                ClientId = "client"
            },
            PostLogOutUri = "http://client/post-logout-callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Single(_mockLogoutMessageStore.Messages);
        var location = _context.Response.Headers["Location"].Single();
        var query = QueryHelpers.ParseQuery(new Uri(location).Query);

        Assert.StartsWith("https://server/logout", location);
        Assert.Equal(_mockLogoutMessageStore.Messages.First().Key, query["logoutId"].First());
    }

    [Fact]
    public async Task unvalidated_signout_should_not_pass_logout_message()
    {
        _result.IsError = false;

        await _subject.ExecuteAsync(_context);

        Assert.Empty(_mockLogoutMessageStore.Messages);
        var location = _context.Response.Headers["Location"].Single();
        var query = QueryHelpers.ParseQuery(new Uri(location).Query);

        Assert.StartsWith("https://server/logout", location);
        Assert.Empty(query);
    }

    [Fact]
    public async Task error_result_should_not_pass_logout_message()
    {
        _result.IsError = true;
        _result.ValidatedRequest = new ValidatedEndSessionRequest
        {
            Client = new Client
            {
                ClientId = "client"
            },
            PostLogOutUri = "http://client/post-logout-callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Empty(_mockLogoutMessageStore.Messages);
        var location = _context.Response.Headers["Location"].Single();
        var query = QueryHelpers.ParseQuery(new Uri(location).Query);

        Assert.StartsWith("https://server/logout", location);
        Assert.Empty(query);
    }
}
