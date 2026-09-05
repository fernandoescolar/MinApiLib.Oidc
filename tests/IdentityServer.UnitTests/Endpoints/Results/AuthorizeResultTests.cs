namespace IdentityServer.UnitTests.Endpoints.Results;

public class AuthorizeResultTests
{
    private AuthorizeResult _subject;

    private AuthorizeResponse _response = new AuthorizeResponse();
    private IdentityServerOptions _options = new IdentityServerOptions();
    private MockUserSession _mockUserSession = new MockUserSession();
    private MockMessageStore<IdentityServer.Models.ErrorMessage> _mockErrorMessageStore = new MockMessageStore<IdentityServer.Models.ErrorMessage>();

    private DefaultHttpContext _context = new DefaultHttpContext();

    public AuthorizeResultTests()
    {
        _context.SetIdentityServerOrigin("https://server");
        _context.SetIdentityServerBasePath("/");
        _context.Response.Body = new MemoryStream();
        _context.RequestServices = new ServiceCollection().AddSingleton(_options).BuildServiceProvider();

        _options.UserInteraction.ErrorUrl = "~/error";
        _options.UserInteraction.ErrorIdParameter = "errorId";

        _subject = new AuthorizeResult(_response, _options, _mockUserSession, _mockErrorMessageStore, new StubClock());
    }

    [Fact]
    public async Task error_should_redirect_to_error_page_and_passs_info()
    {
        _response.Error = "some_error";

        await _subject.ExecuteAsync(_context);

        Assert.Single(_mockErrorMessageStore.Messages);
        Assert.Equal(302, _context.Response.StatusCode);
        var location = _context.Response.Headers["Location"].First();
        Assert.StartsWith("https://server/error", location);
        var query = QueryHelpers.ParseQuery(new Uri(location).Query);
        Assert.Equal(_mockErrorMessageStore.Messages.First().Key, query["errorId"].First());
    }

    [Theory]
    [InlineData(OidcConstants.AuthorizeErrors.AccountSelectionRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.LoginRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.ConsentRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.InteractionRequired)]
    public async Task prompt_none_errors_should_return_to_client(string error)
    {
        _response.Error = error;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback",
            PromptModes = new[] { "none" }
        };

        await _subject.ExecuteAsync(_context);

        Assert.Empty(_mockUserSession.Clients);
        Assert.Equal(302, _context.Response.StatusCode);
        var location = _context.Response.Headers["Location"].First();
        Assert.StartsWith("http://client/callback", location);
    }

    [Theory]
    [InlineData(OidcConstants.AuthorizeErrors.AccountSelectionRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.LoginRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.ConsentRequired)]
    [InlineData(OidcConstants.AuthorizeErrors.InteractionRequired)]
    public async Task prompt_none_errors_for_anonymous_users_should_include_session_state(string error)
    {
        _response.Error = error;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback",
            PromptModes = new[] { "none" },
        };
        _response.SessionState = "some_session_state";

        await _subject.ExecuteAsync(_context);

        Assert.Empty(_mockUserSession.Clients);
        Assert.Equal(302, _context.Response.StatusCode);
        var location = _context.Response.Headers["Location"].First();
        Assert.Contains("session_state=some_session_state", location);
    }

    [Fact]
    public async Task access_denied_should_return_to_client()
    {
        const string errorDescription = "some error description";

        _response.Error = OidcConstants.AuthorizeErrors.AccessDenied;
        _response.ErrorDescription = errorDescription;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Empty(_mockUserSession.Clients);
        Assert.Equal(302, _context.Response.StatusCode);
        var location = _context.Response.Headers["Location"].First();
        Assert.StartsWith("http://client/callback", location);

        var queryString = new Uri(location).Query;
        var queryParams = QueryHelpers.ParseQuery(queryString);

        Assert.Equal(OidcConstants.AuthorizeErrors.AccessDenied, queryParams["error"]);
        Assert.Equal(errorDescription, queryParams["error_description"]);
    }

    [Fact]
    public async Task success_should_add_client_to_client_list()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Contains("client", _mockUserSession.Clients);
    }

    [Fact]
    public async Task query_mode_should_pass_results_in_query()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Equal(302, _context.Response.StatusCode);
        Assert.Contains("no-store", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("no-cache", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("max-age=0", _context.Response.Headers["Cache-Control"].First());
        var location = _context.Response.Headers["Location"].First();
        Assert.StartsWith("http://client/callback", location);
        Assert.Contains("?state=state", location);
    }

    [Fact]
    public async Task fragment_mode_should_pass_results_in_fragment()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Fragment,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Equal(302, _context.Response.StatusCode);
        Assert.Contains("no-store", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("no-cache", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("max-age=0", _context.Response.Headers["Cache-Control"].First());
        var location = _context.Response.Headers["Location"].First();
        Assert.StartsWith("http://client/callback", location);
        Assert.Contains("#state=state", location);
    }

    [Fact]
    public async Task form_post_mode_should_pass_results_in_body()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.FormPost,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Equal(200, _context.Response.StatusCode);
        Assert.StartsWith("text/html", _context.Response.ContentType);
        Assert.Contains("no-store", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("no-cache", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("max-age=0", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("default-src 'none';", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("default-src 'none';", _context.Response.Headers["X-Content-Security-Policy"].First());
        Assert.Contains("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='", _context.Response.Headers["X-Content-Security-Policy"].First());
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var rdr = new StreamReader(_context.Response.Body))
        {
            var html = await rdr.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Contains("<base target='_self'/>", html);
            Assert.Contains("<form method='post' action='http://client/callback'>", html);
            Assert.Contains("<input type='hidden' name='state' value='state' />", html);
        }
    }

    [Fact]
    public async Task form_post_mode_should_add_unsafe_inline_for_csp_level_1()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.FormPost,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        _options.Csp.Level = CspLevel.One;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("script-src 'unsafe-inline' 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("script-src 'unsafe-inline' 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='", _context.Response.Headers["X-Content-Security-Policy"].First());
    }

    [Fact]
    public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.FormPost,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        _options.Csp.AddDeprecatedHeader = false;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("script-src 'sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.True(StringValues.IsNullOrEmpty(_context.Response.Headers["X-Content-Security-Policy"]));
    }

    [Fact]
    public async Task success_response_should_include_iss_parameter()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback",
            State = "state"
        };

        await _subject.ExecuteAsync(_context);

        var query = QueryHelpers.ParseQuery(new Uri(_context.Response.Headers["Location"].First()).Query);
        Assert.Equal("https://server", query["iss"].First());
    }

    [Fact]
    public async Task safe_error_response_should_include_iss_parameter()
    {
        _response.Error = OidcConstants.AuthorizeErrors.AccessDenied;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        var query = QueryHelpers.ParseQuery(new Uri(_context.Response.Headers["Location"].First()).Query);
        Assert.Equal("https://server", query["iss"].First());
    }

    [Fact]
    public async Task iss_parameter_should_be_omitted_when_disabled()
    {
        _options.EmitIssuerIdentificationResponseParameter = false;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.DoesNotContain("iss=", _context.Response.Headers["Location"].First());
    }

    [Fact]
    public async Task form_post_mode_should_include_iss_field()
    {
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.FormPost,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        var html = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Contains("<input type='hidden' name='iss' value='https://server' />", html);
    }

    [Fact]
    public async Task post_request_should_redirect_with_see_other()
    {
        _context.Request.Method = HttpMethods.Post;
        _response.Request = new ValidatedAuthorizeRequest
        {
            ClientId = "client",
            ResponseMode = OidcConstants.ResponseModes.Query,
            RedirectUri = "http://client/callback"
        };

        await _subject.ExecuteAsync(_context);

        Assert.Equal(StatusCodes.Status303SeeOther, _context.Response.StatusCode);
    }

    [Fact]
    public async Task post_request_error_page_redirect_should_use_see_other()
    {
        _context.Request.Method = HttpMethods.Post;
        _response.Error = "some_error";

        await _subject.ExecuteAsync(_context);

        Assert.Equal(StatusCodes.Status303SeeOther, _context.Response.StatusCode);
        Assert.StartsWith("https://server/error", _context.Response.Headers["Location"].First());
    }
}
