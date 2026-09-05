namespace IdentityServer.UnitTests.Validation;

public class StrictRedirectUriValidatorAppAuthValidation
{
    private const string Category = "Strict Redirect Uri Validator AppAuth Validation Tests";

    private readonly Client clientWithValidLoopbackRedirectUri = new Client
    {
        RequirePkce = true,
        RedirectUris = new List<string>
        {
            "http://127.0.0.1"
        }
    };

    [Theory]
    [Trait("Category", Category)]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.0.0.1:80")]
    [InlineData("http://127.0.0.1:65535")]
    [InlineData("http://127.0.0.1:123/a/b")]
    [InlineData("http://127.0.0.1:123?q=123")]
    [InlineData("http://127.0.0.1:443/?q=123")]
    [InlineData("http://127.0.0.1:443/a/b?q=123")]
    public async Task Loopback_Redirect_URIs_Should_Be_AllowedAsync(string requestedUri)
    {
        var strictRedirectUriValidatorAppAuthValidator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());

        var result = await strictRedirectUriValidatorAppAuthValidator.IsRedirectUriValidAsync(requestedUri, clientWithValidLoopbackRedirectUri);

        Assert.True(result);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http:")]
    [InlineData("127.0.0.1")]
    [InlineData("//127.0.0.1")]
    [InlineData("https://127.0.0.1:123")]
    [InlineData("http://127.0.0.1:")]
    [InlineData("http://127.0.0.1:-1")]
    [InlineData("http://127.0.0.2:65536")]
    [InlineData("http://127.0.0.1:443a")]
    [InlineData("http://127.0.0.1:a443")]
    [InlineData("http://127.0.0.1:443a/")]
    [InlineData("http://127.0.0.1:443a?")]
    [InlineData("http://127.0.0.2:443")]
    [InlineData("http://127.0.0.1#abc")]
    [InlineData("http://127.0.0.1:#abc")]
    [InlineData("http://127.0.0.1:0")]
    [InlineData("http://127.0.0.1:443#abc")]
    [InlineData("http://127.0.0.1:443/a/b?q=123#abc")]
    [InlineData("http://user@127.0.0.1:443/cb")]
    [InlineData("http://localhost:443/cb")]
    public async Task Loopback_Redirect_URIs_Should_Not_Be_AllowedAsync(string requestedUri)
    {
        var strictRedirectUriValidatorAppAuthValidator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());

        var result = await strictRedirectUriValidatorAppAuthValidator.IsRedirectUriValidAsync(requestedUri, clientWithValidLoopbackRedirectUri);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Loopback_post_logout_redirect_uri_without_registered_post_logout_loopback_should_not_be_allowed()
    {
        var validator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());

        // The client registers the loopback only as a redirect URI, not as a post-logout redirect URI.
        var result = await validator.IsPostLogoutRedirectUriValidAsync("http://127.0.0.1:123", clientWithValidLoopbackRedirectUri);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Loopback_post_logout_redirect_uri_with_registered_post_logout_loopback_should_be_allowed()
    {
        var validator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());
        var client = new Client
        {
            RequirePkce = true,
            PostLogoutRedirectUris = { "http://127.0.0.1" }
        };

        var result = await validator.IsPostLogoutRedirectUriValidAsync("http://127.0.0.1:123", client);

        Assert.True(result);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("http://127.0.0.1:51004/callback", true)]
    [InlineData("http://127.0.0.1:1/callback", true)]
    [InlineData("http://127.0.0.1/callback", true)]
    [InlineData("http://127.0.0.1:51004/other", false)]
    [InlineData("http://127.0.0.1:51004/callback?x=1", false)]
    [InlineData("http://127.0.0.1:51004/CALLBACK", false)]
    [InlineData("http://[::1]:51004/callback", false)]
    public async Task Loopback_registration_with_path_allows_any_port_but_requires_same_path(string requestedUri, bool expected)
    {
        var validator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());
        var client = new Client { RequirePkce = true, RedirectUris = { "http://127.0.0.1/callback" } };

        var result = await validator.IsRedirectUriValidAsync(requestedUri, client);

        Assert.Equal(expected, result);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("http://[::1]:51004", true)]
    [InlineData("http://[::1]:51004/any/path", true)]
    [InlineData("http://127.0.0.1:51004", false)]
    public async Task Ipv6_loopback_registration_should_be_supported(string requestedUri, bool expected)
    {
        var validator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());
        var client = new Client { RequirePkce = true, RedirectUris = { "http://[::1]" } };

        var result = await validator.IsRedirectUriValidAsync(requestedUri, client);

        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Loopback_should_not_be_allowed_without_pkce()
    {
        var validator = new StrictRedirectUriValidatorAppAuth(TestLogger.Create<StrictRedirectUriValidatorAppAuth>());
        var client = new Client { RequirePkce = false, RedirectUris = { "http://127.0.0.1" } };

        var result = await validator.IsRedirectUriValidAsync("http://127.0.0.1:51004", client);

        Assert.False(result);
    }
}
