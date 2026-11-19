using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace IdP.Features.Flow;

/// <summary>
/// The user whose password was accepted and who must now give a second factor: in an encrypted cookie for the
/// <c>/login/mfa</c> page, valid for five minutes.
/// </summary>
public sealed class MfaState(IDataProtectionProvider dataProtection)
{
    private const string CookieName = "idp.mfa";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly ITimeLimitedDataProtector _protector = dataProtection.CreateProtector("IdP.Login.Mfa").ToTimeLimitedDataProtector();

    public sealed record Pending(string UserId, string? ReturnUrl);

    public void Save(HttpContext http, Pending pending)
        => http.Response.Cookies.Append(CookieName, _protector.Protect(JsonSerializer.Serialize(pending), Lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/login",
            MaxAge = Lifetime,
            IsEssential = true
        });

    public Pending? Peek(HttpContext http)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out var value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Pending>(_protector.Unprotect(value));
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Clear(HttpContext http) => http.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/login", Secure = http.Request.IsHttps });
}
