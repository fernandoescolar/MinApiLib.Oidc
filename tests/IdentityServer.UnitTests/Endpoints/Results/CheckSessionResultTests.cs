namespace IdentityServer.UnitTests.Endpoints.Results;

public class CheckSessionResultTests
{
    private CheckSessionResult _subject;

    private IdentityServerOptions _options = new IdentityServerOptions();

    private DefaultHttpContext _context = new DefaultHttpContext();

    public CheckSessionResultTests()
    {
        _context.SetIdentityServerOrigin("https://server");
        _context.SetIdentityServerBasePath("/");
        _context.Response.Body = new MemoryStream();

        _options.Authentication.CheckSessionCookieName = "foobar";

        _subject = new CheckSessionResult(_options);
    }

    [Fact]
    public async Task should_pass_results_in_body()
    {
        await _subject.ExecuteAsync(_context);

        Assert.Equal(200, _context.Response.StatusCode);
        Assert.StartsWith("text/html", _context.Response.ContentType);
        Assert.Contains("default-src 'none';", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("default-src 'none';", _context.Response.Headers["X-Content-Security-Policy"].First());
        Assert.Contains("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='", _context.Response.Headers["X-Content-Security-Policy"].First());
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var rdr = new StreamReader(_context.Response.Body))
        {
            var html = await rdr.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Contains("<script id='cookie-name' type='application/json'>foobar</script>", html);
        }
    }

    [Fact]
    public async Task form_post_mode_should_add_unsafe_inline_for_csp_level_1()
    {
        _options.Csp.Level = CspLevel.One;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("script-src 'unsafe-inline' 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.Contains("script-src 'unsafe-inline' 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='", _context.Response.Headers["X-Content-Security-Policy"].First());
    }

    [Fact]
    public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
    {
        _options.Csp.AddDeprecatedHeader = false;

        await _subject.ExecuteAsync(_context);

        Assert.Contains("script-src 'sha256-fa5rxHhZ799izGRP38+h4ud5QXNT0SFaFlh4eqDumBI='", _context.Response.Headers["Content-Security-Policy"].First());
        Assert.True(StringValues.IsNullOrEmpty(_context.Response.Headers["X-Content-Security-Policy"]));
    }

    [Theory]
    [InlineData("foobar")]
    [InlineData("morefoobar")]

    public async Task can_change_cached_cookiename(string cookieName)
    {
        _options.Authentication.CheckSessionCookieName = cookieName;
        await _subject.ExecuteAsync(_context);
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var rdr = new StreamReader(_context.Response.Body))
        {
            var html = await rdr.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Contains($"<script id='cookie-name' type='application/json'>{cookieName}</script>", html);
        }
    }
}
