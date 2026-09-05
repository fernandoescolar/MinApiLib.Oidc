namespace IdentityServer.UnitTests.Validation.Secrets;

public class BasicAuthenticationSecretParsing
{
    private const string Category = "Secrets - Basic Authentication Secret Parsing";

    private IdentityServerOptions _options;
    private BasicAuthenticationSecretParser _parser;

    public BasicAuthenticationSecretParsing()
    {
        _options = new IdentityServerOptions();
        _parser = new BasicAuthenticationSecretParser(_options, TestLogger.Create<BasicAuthenticationSecretParser>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task EmptyContext()
    {
        var context = new DefaultHttpContext();

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_BasicAuthentication_Request()
    {
        var context = new DefaultHttpContext();

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client:secret")));
        context.Request.Headers.Append("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);

        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.SharedSecret, secret.Type);
        Assert.Equal("client", secret.Id);
        Assert.Equal("secret", secret.Credential);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_BasicAuthentication_Request_With_UserName_Only_And_Colon_For_Optional_ClientSecret()
    {
        var context = new DefaultHttpContext();

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client:")));
        context.Request.Headers.Append("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);

        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.NoSecret, secret.Type);
        Assert.Equal("client", secret.Id);
        Assert.Null(secret.Credential);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task BasicAuthentication_Request_With_Empty_Basic_Header()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Append("Authorization", new StringValues(string.Empty));

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_BasicAuthentication_Request_ClientId_Too_Long()
    {
        var context = new DefaultHttpContext();

        var longClientId = "x".Repeat(_options.InputLengthRestrictions.ClientId + 1);
        var credential = string.Format("{0}:secret", longClientId);

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)));
        context.Request.Headers.Append("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);
        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_BasicAuthentication_Request_ClientSecret_Too_Long()
    {
        var context = new DefaultHttpContext();

        var longClientSecret = "x".Repeat(_options.InputLengthRestrictions.ClientSecret + 1);
        var credential = string.Format("client:{0}", longClientSecret);

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(credential)));
        context.Request.Headers.Append("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);
        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task BasicAuthentication_Request_With_Empty_Basic_Header_Variation()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Append("Authorization", new StringValues("Basic "));

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task BasicAuthentication_Request_With_Unknown_Scheme()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Append("Authorization", new StringValues("Unknown"));

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task BasicAuthentication_Request_With_Malformed_Credentials_NoBase64_Encoding()
    {
        var context = new DefaultHttpContext();

        context.Request.Headers.Append("Authorization", new StringValues("Basic somerandomdata"));

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task BasicAuthentication_Request_With_Malformed_Credentials_Base64_Encoding_UserName_Only()
    {
        var context = new DefaultHttpContext();

        var headerValue = string.Format("Basic {0}",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("client")));
        context.Request.Headers.Append("Authorization", new StringValues(headerValue));

        var secret = await _parser.ParseAsync(context);

        Assert.Null(secret);
    }
}
