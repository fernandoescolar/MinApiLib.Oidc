using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Samples.Server.Passkeys;

/// <summary>
/// Keeps the state of a passkey ceremony, with its challenge, between the options and the browser's answer: in a
/// cookie for <c>/account/passkey</c>, encrypted and valid for five minutes. Each state is read once.
/// </summary>
public sealed class PasskeyState(IDataProtectionProvider dataProtection)
{
    public const string Assertion = "assertion";
    public const string Attestation = "attestation";

    private const string CookiePrefix = "sample.passkey.";
    private const string CookiePath = "/account/passkey";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public void Save(HttpContext http, string ceremony, string state)
        => http.Response.Cookies.Append(CookiePrefix + ceremony, Protector(ceremony).Protect(state, Lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            MaxAge = Lifetime,
            IsEssential = true
        });

    public string? Take(HttpContext http, string ceremony)
    {
        if (!http.Request.Cookies.TryGetValue(CookiePrefix + ceremony, out var protectedState))
        {
            return null;
        }

        http.Response.Cookies.Delete(CookiePrefix + ceremony, new CookieOptions { Path = CookiePath, Secure = true });
        try
        {
            return Protector(ceremony).Unprotect(protectedState);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private ITimeLimitedDataProtector Protector(string ceremony)
        => dataProtection.CreateProtector("Samples.Server.Passkeys", ceremony).ToTimeLimitedDataProtector();
}
