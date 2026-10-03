namespace MinApiLib.Oidc.Server.Core.Extensions;

/// <summary>
/// Extensions methods for X509Certificate2
/// </summary>
public static class X509CertificateExtensions
{
    /// <summary>
    /// Create the value of a thumbprint-based cnf claim
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public static string CreateThumbprintCnf(this X509Certificate2 certificate)
    {
        var hash = certificate.GetCertHash(HashAlgorithmName.SHA256);

        var values = new Dictionary<string, string>
            {
                { "x5t#S256", Base64Url.EncodeToString(hash) }
            };

        return System.Text.Json.JsonSerializer.Serialize(values);
    }
}
