namespace IdentityServer.UnitTests.Validation.Secrets;

public class MutualTlsSecretParsing
{
    private const string Category = "Secrets - mTLS parsing";

    // Matches Constants.EnvironmentKeys.MutualTlsClientCertificateAuthenticated.
    private const string AuthenticatedMarker = "idsvr:MutualTlsClientCertificateAuthenticated";

    private static DefaultHttpContext CreateContext(bool authenticatedByMtlsEndpoint)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("client_id=mtls_client"));
        context.Connection.ClientCertificate = TestCert.Load();
        if (authenticatedByMtlsEndpoint)
        {
            context.Items[AuthenticatedMarker] = true;
        }

        return context;
    }

    private static MutualTlsSecretParser CreateParser(bool mtlsEnabled = true)
    {
        var options = new IdentityServerOptions();
        options.MutualTls.Enabled = mtlsEnabled;
        return new MutualTlsSecretParser(options, TestLogger.Create<MutualTlsSecretParser>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Certificate_not_authenticated_by_mtls_endpoint_should_be_ignored()
    {
        var secret = await CreateParser().ParseAsync(CreateContext(authenticatedByMtlsEndpoint: false));

        Assert.Null(secret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Certificate_authenticated_by_mtls_endpoint_should_be_parsed()
    {
        var secret = await CreateParser().ParseAsync(CreateContext(authenticatedByMtlsEndpoint: true));

        Assert.NotNull(secret);
        Assert.Equal("mtls_client", secret.Id);
        Assert.Equal(IdentityServerConstants.ParsedSecretTypes.X509Certificate, secret.Type);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Certificate_should_be_ignored_when_mtls_is_disabled()
    {
        var secret = await CreateParser(mtlsEnabled: false).ParseAsync(CreateContext(authenticatedByMtlsEndpoint: true));

        Assert.Null(secret);
    }
}
