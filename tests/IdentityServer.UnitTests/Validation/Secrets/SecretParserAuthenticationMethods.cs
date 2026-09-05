namespace IdentityServer.UnitTests.Validation.Secrets;

public class SecretParserAuthenticationMethods
{
    private const string Category = "Secret parser - authentication methods";

    private static SecretParser CreateParser()
    {
        var options = new IdentityServerOptions();
        return new SecretParser(
            [
                new BasicAuthenticationSecretParser(options, TestLogger.Create<BasicAuthenticationSecretParser>()),
                new PostBodySecretParser(options, TestLogger.Create<PostBodySecretParser>())
            ],
            TestLogger.Create<ISecretsListParser>());
    }

    private static DefaultHttpContext CreateContext(string body, string basicCredentials = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        if (basicCredentials != null)
        {
            context.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(basicCredentials));
        }

        return context;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Basic_and_post_body_secret_together_should_be_rejected()
    {
        var secret = await CreateParser().ParseAsync(CreateContext("client_id=client&client_secret=secret", "client:secret"));

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Basic_with_client_id_in_body_should_be_accepted()
    {
        var secret = await CreateParser().ParseAsync(CreateContext("client_id=client", "client:secret"));

        Assert.NotNull(secret);
        Assert.Equal("client", secret.Id);
        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.SharedSecret, secret.Type);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Post_body_secret_alone_should_be_accepted()
    {
        var secret = await CreateParser().ParseAsync(CreateContext("client_id=client&client_secret=secret"));

        Assert.NotNull(secret);
        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.SharedSecret, secret.Type);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Public_client_without_secret_should_be_accepted()
    {
        var secret = await CreateParser().ParseAsync(CreateContext("client_id=client"));

        Assert.NotNull(secret);
        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.NoSecret, secret.Type);
    }
}
