namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

internal static class TestCert
{
    public static X509Certificate2 Load()
    {
        var cert = Path.Combine(System.AppContext.BaseDirectory, "identityserver_testing.pfx");
        return X509CertificateLoader.LoadPkcs12FromFile(cert, "password");
    }
}
