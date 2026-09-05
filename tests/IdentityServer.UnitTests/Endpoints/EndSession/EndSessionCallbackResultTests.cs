namespace IdentityServer.UnitTests.Endpoints.EndSession;

public class EndSessionCallbackResultTests
{
    private const string Category = "End Session Callback Result";

    private readonly EndSessionCallbackValidationResult _validationResult;
    private readonly IdentityServerOptions _options;
    private readonly EndSessionCallbackResult _subject;

    public EndSessionCallbackResultTests()
    {
        _validationResult = new EndSessionCallbackValidationResult()
        {
            IsError = false,
        };
        _options = new IdentityServerOptions();
        _subject = new EndSessionCallbackResult(_validationResult, _options);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task default_options_should_emit_frame_src_csp_headers()
    {
        _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";

        await _subject.ExecuteAsync(ctx);

        Assert.Contains("frame-src http://foo", ctx.Response.Headers["Content-Security-Policy"].First());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task relax_csp_options_should_prevent_frame_src_csp_headers()
    {
        _options.Authentication.RequireCspFrameSrcForSignout = false;
        _validationResult.FrontChannelLogoutUrls = new[] { "http://foo" };

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";

        await _subject.ExecuteAsync(ctx);

        Assert.Null(ctx.Response.Headers["Content-Security-Policy"].FirstOrDefault());
    }
}
