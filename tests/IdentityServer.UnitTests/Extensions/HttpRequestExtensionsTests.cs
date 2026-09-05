namespace IdentityServer.UnitTests.Extensions;

public class HttpRequestExtensionsTests
{
    [Fact]
    public void GetCorsOrigin_valid_cors_request_should_return_cors_origin()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("foo");
        ctx.Request.Headers.Append("Origin", "http://bar");

        Assert.Equal("http://bar", ctx.Request.GetCorsOrigin());
    }

    [Fact]
    public void GetCorsOrigin_origin_from_same_host_should_not_return_cors_origin()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("foo");
        ctx.Request.Headers.Append("Origin", "http://foo");

        Assert.Null(ctx.Request.GetCorsOrigin());
    }

    [Fact]
    public void GetCorsOrigin_no_origin_should_not_return_cors_origin()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("foo");

        Assert.Null(ctx.Request.GetCorsOrigin());
    }
}
