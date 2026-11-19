using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace IdP.Features.Flow;

/// <summary>
/// Keeps the state of a passkey ceremony, with its challenge, between the options and the browser's answer: in an
/// encrypted cookie valid for five minutes, read once.
/// </summary>
public sealed class PasskeyState(IDataProtectionProvider dataProtection)
{
    public const string Assertion = "assertion";
    public const string Attestation = "attestation";

    private const string CookiePrefix = "idp.passkey.";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public void Save(HttpContext http, string ceremony, string state)
        => http.Response.Cookies.Append(CookiePrefix + ceremony, Protector(ceremony).Protect(state, Lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = Lifetime,
            IsEssential = true
        });

    public string? Take(HttpContext http, string ceremony)
    {
        if (!http.Request.Cookies.TryGetValue(CookiePrefix + ceremony, out var protectedState))
        {
            return null;
        }

        http.Response.Cookies.Delete(CookiePrefix + ceremony, new CookieOptions { Path = "/", Secure = http.Request.IsHttps });
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
        => dataProtection.CreateProtector("IdP.Passkeys", ceremony).ToTimeLimitedDataProtector();
}
