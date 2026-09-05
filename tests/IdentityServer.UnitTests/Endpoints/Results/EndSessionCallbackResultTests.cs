namespace IdentityServer.UnitTests.Endpoints.Results;

public class EndSessionCallbackResultTests
{
    private EndSessionCallbackResult _subject;

    private EndSessionCallbackValidationResult _result = new EndSessionCallbackValidationResult();
    private IdentityServerOptions _options = TestIdentityServerOptions.Create();

    private DefaultHttpContext _context = new DefaultHttpContext();

    public EndSessionCallbackResultTests()
    {
        _context.SetIdentityServerOrigin("https://server");
        _context.SetIdentityServerBasePath("/");
        _context.Response.Body = new MemoryStream();

        _subject = new EndSessionCallbackResult(_result, _options);
    }

    [Fact]
    public async Task error_should_return_400()
    {
        _result.IsError = true;

        await _subject.ExecuteAsync(_context);

        Assert.Equal(400, _context.Response.StatusCode);
    }

    [Fact]
    public async Task success_should_render_html_and_iframes()
    {
        _result.IsError = false;
        _result.FrontChannelLogoutUrls = new string[] { "http://foo.com", "http://bar.com" };

        await _subject.ExecuteAsync(_context);

        Assert.StartsWith("text/html", _context.Response.ContentType);
        Assert.Contains("no-store", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("no-cache", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("max-age=0", _context.Response.Headers["Cache-Control"].First());
        Assert.Contains("default-src 'none';", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("style-src 'sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY=';", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("frame-src http://foo.com http://bar.com", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("default-src 'none';", _context.Response.Headers["X-Content-Security-Policy"].First());
        Assert.Contains("style-src 'sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY=';", _context.Response.Headers["X-Content-Security-Policy"].First());
        Assert.Contains("frame-src http://foo.com http://bar.com", _context.Response.Headers["X-Content-Security-Policy"].First());
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var rdr = new StreamReader(_context.Response.Body))
        {
            var html = await rdr.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Contains("<iframe src='http://foo.com'></iframe>", html);
            Assert.Contains("<iframe src='http://bar.com'></iframe>", html);
        }
    }

    [Fact]
    public async Task fsuccess_should_add_unsafe_inline_for_csp_level_1()
    {
        _result.IsError = false;

        _options.Csp.Level = CspLevel.One;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("style-src 'unsafe-inline' 'sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("style-src 'unsafe-inline' 'sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY='", _context.Response.Headers["X-Content-Security-Policy"].First());
    }

    [Fact]
    public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
    {
        _result.IsError = false;

        _options.Csp.AddDeprecatedHeader = false;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("style-src 'sha256-u+OupXgfekP+x/f6rMdoEAspPCYUtca912isERnoEjY='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.True(StringValues.IsNullOrEmpty(_context.Response.Headers["X-Content-Security-Policy"]));
    }
}
