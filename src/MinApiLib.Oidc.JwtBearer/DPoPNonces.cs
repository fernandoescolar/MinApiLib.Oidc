namespace MinApiLib.Oidc.JwtBearer;

/// <summary>
/// Nonces the API issues without storing them: encrypted, time-limited values only it can read.
/// </summary>
internal sealed class DPoPNonces(IDataProtectionProvider dataProtection)
{
    private readonly ITimeLimitedDataProtector _protector =
        dataProtection.CreateProtector("MinApiLib.Oidc.JwtBearer.DPoPNonce").ToTimeLimitedDataProtector();

    public string Create(TimeSpan lifetime) => _protector.Protect(Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16)), lifetime);

    public bool IsValid(string? nonce)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        try
        {
            _protector.Unprotect(nonce);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
