namespace IdentityServer.UnitTests.Common;

internal static class TestCert
{
    public static X509Certificate2 Load()
    {
        var cert = Path.Combine(System.AppContext.BaseDirectory, "identityserver_testing.pfx");

        return X509CertificateLoader.LoadPkcs12FromFile(cert, "password");
    }

    public static SigningCredentials LoadSigningCredentials()
    {
        var cert = Load();
        return new SigningCredentials(new X509SecurityKey(cert), "RS256");
    }
}
