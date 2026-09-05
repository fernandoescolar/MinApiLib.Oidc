namespace IdentityServer.UnitTests.Services.Default;

public class JwtRequestUriHttpMessageHandlerTests
{
    private const string Category = "request_uri SSRF protection";

    [Theory]
    [Trait("Category", Category)]
    [InlineData("0.1.2.3")]
    [InlineData("10.0.0.5")]
    [InlineData("100.64.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.0.0.8")]
    [InlineData("192.0.2.1")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.51.100.7")]
    [InlineData("203.0.113.9")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("64:ff9b::a00:1")]
    public void Private_and_reserved_addresses_are_blocked(string address)
    {
        Assert.True(JwtRequestUriHttpMessageHandler.IsPrivateOrReservedIp(IPAddress.Parse(address)));
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")]
    [InlineData("192.169.0.1")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::808:808")]
    public void Public_addresses_are_allowed(string address)
    {
        Assert.False(JwtRequestUriHttpMessageHandler.IsPrivateOrReservedIp(IPAddress.Parse(address)));
    }

    [Fact]
    [Trait("Category", Category)]
    public void Handler_does_not_follow_redirects()
    {
        using var handler = JwtRequestUriHttpMessageHandler.Create();

        Assert.False(handler.AllowAutoRedirect);
        Assert.NotNull(handler.ConnectCallback);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Host_names_resolving_to_loopback_are_refused_at_connect_time()
    {
        using var client = new HttpClient(JwtRequestUriHttpMessageHandler.Create());

        var act = () => client.GetAsync("https://localhost/request.jwt", TestContext.Current.CancellationToken);

        Assert.Contains("does not resolve to a public IP address", (await Assert.ThrowsAnyAsync<HttpRequestException>(act)).ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Fetch_failure_returns_null_instead_of_throwing()
    {
        using var httpClient = new HttpClient(new ThrowingHandler());
        var client = new DefaultJwtRequestUriHttpClient(httpClient, new IdentityServerOptions(), NullLoggerFactory.Instance);

        var jwt = await client.GetJwtAsync("https://client.example/request.jwt", new Client(), TestContext.Current.CancellationToken);

        Assert.Null(jwt);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(new HttpRequestException("connection refused"));
    }
}
