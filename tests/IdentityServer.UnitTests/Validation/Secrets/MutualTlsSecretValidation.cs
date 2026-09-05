namespace IdentityServer.UnitTests.Validation.Secrets;

public class MutualTlsSecretValidation
{
    private const string Category = "Secrets - MutualTls Secret Validation";

    private IClientStore _clients = new InMemoryClientStore(ClientValidationTestClients.Get());

    [Fact]
    [Trait("Category", Category)]
    public async Task Thumbprint_invalid_secret_type_should_not_match()
    {
        ISecretValidator validator = new X509ThumbprintSecretValidator(new Logger<X509ThumbprintSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = "secret",
            Type = IdentityServerConstants.ParsedSecretTypes.SharedSecret
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Thumbprint_missing_cert_should_throw()
    {
        ISecretValidator validator = new X509ThumbprintSecretValidator(new Logger<X509ThumbprintSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = "secret",
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        Func<Task> act = async () => await validator.ValidateAsync(client.ClientSecrets, secret);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Thumbprint_invalid_secret_should_not_match()
    {
        ISecretValidator validator = new X509ThumbprintSecretValidator(new Logger<X509ThumbprintSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = TestCert.Load(),
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Thumbprint_valid_secret_should_match()
    {
        ISecretValidator validator = new X509ThumbprintSecretValidator(new Logger<X509ThumbprintSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_valid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = TestCert.Load(),
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.True(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Name_invalid_secret_type_should_not_match()
    {
        ISecretValidator validator = new X509NameSecretValidator(new Logger<X509NameSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = "secret",
            Type = IdentityServerConstants.ParsedSecretTypes.SharedSecret
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Name_missing_cert_should_throw()
    {
        ISecretValidator validator = new X509NameSecretValidator(new Logger<X509NameSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = "secret",
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        Func<Task> act = async () => await validator.ValidateAsync(client.ClientSecrets, secret);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(act);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Name_invalid_secret_should_not_match()
    {
        ISecretValidator validator = new X509NameSecretValidator(new Logger<X509NameSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_invalid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = TestCert.Load(),
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Name_valid_secret_should_match()
    {
        ISecretValidator validator = new X509NameSecretValidator(new Logger<X509NameSecretValidator>(new LoggerFactory()));

        var clientId = "mtls_client_valid";
        var client = await _clients.FindEnabledClientByIdAsync(clientId);

        var secret = new ParsedSecret
        {
            Id = clientId,
            Credential = TestCert.Load(),
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        var result = await validator.ValidateAsync(client.ClientSecrets, secret);

        Assert.True(result.Success);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Thumbprint_sha256_secret_should_match()
    {
        ISecretValidator validator = new X509ThumbprintSecretValidator(new Logger<X509ThumbprintSecretValidator>(new LoggerFactory()));
        var certificate = TestCert.Load();

        var secrets = new[]
        {
            new Secret
            {
                Type = IdentityServerConstants.SecretTypes.X509CertificateThumbprint,
                Value = certificate.GetCertHashString(HashAlgorithmName.SHA256)
            }
        };
        var secret = new ParsedSecret
        {
            Id = "client",
            Credential = certificate,
            Type = IdentityServerConstants.ParsedSecretTypes.X509Certificate
        };

        var result = await validator.ValidateAsync(secrets, secret);

        Assert.True(result.Success);
    }
}
