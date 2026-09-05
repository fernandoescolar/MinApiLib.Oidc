namespace IdentityServer.UnitTests.Services.Default;

public class DefaultIssuerNameServiceTests
{
    [Fact]
    public async Task Configured_issuer_is_used_without_an_http_request()
    {
        var options = new IdentityServerOptions { IssuerUri = "https://issuer.example" };
        var service = new DefaultIssuerNameService(options, new HttpContextAccessor());

        Assert.Equal("https://issuer.example", (await service.GetCurrentAsync()));
    }

    [Fact]
    public async Task Issuer_is_derived_from_the_request_when_not_configured()
    {
        var options = new IdentityServerOptions();
        var accessor = new MockHttpContextAccessor(options);
        accessor.HttpContext.SetIdentityServerOrigin("https://SERVER.example");
        accessor.HttpContext.SetIdentityServerBasePath("/identity");

        var issuer = await new DefaultIssuerNameService(options, accessor).GetCurrentAsync();

        Assert.Equal("https://server.example/identity", issuer);
    }

    [Fact]
    public Task Missing_issuer_outside_a_request_should_throw()
    {
        var service = new DefaultIssuerNameService(new IdentityServerOptions(), new HttpContextAccessor());

        var act = () => service.GetCurrentAsync();

        return Assert.ThrowsAnyAsync<InvalidOperationException>(act);
    }
}
