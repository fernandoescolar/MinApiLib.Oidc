using Microsoft.AspNetCore.Authentication.Cookies;

namespace IdP.Users;

/// <summary>
/// Checks the session cookie against the user document on each request: a user who was disabled, deleted, or whose
/// security stamp changed (password reset, "sign out everywhere", administrator action) loses the session.
/// </summary>
public static class SecurityStampValidation
{
    public const string ClaimType = "security_stamp";

    /// <summary>Set in <c>HttpContext.Items</c> by a request that ends the session on purpose, so it is not renewed.</summary>
    public const string SkipRefresh = "idp.session.skip-refresh";

    /// <summary>
    /// Many account operations renew the security stamp (password, authenticator, e-mail, external logins). Called
    /// after one, this re-issues the session cookie with the new stamp, so the user who made the change stays signed in.
    /// </summary>
    public static async Task RefreshAsync(HttpContext http)
    {
        if (http.Items.ContainsKey(SkipRefresh) || http.User.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
        {
            return;
        }

        var subject = identity.FindFirst(JwtClaimTypes.Subject)?.Value;
        var stamp = identity.FindFirst(ClaimType)?.Value;
        if (subject == null || stamp == null)
        {
            return;
        }

        var users = http.RequestServices.GetRequiredService<IdPMongoContext>();
        var user = await users.Users.Find(u => u.Id == subject).FirstOrDefaultAsync();
        if (user == null || !IdPProfileService.IsActive(user) || string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            return;
        }

        var claims = identity.Claims.Where(c => c.Type != ClaimType).Append(new Claim(ClaimType, user.SecurityStamp)).ToList();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType));
        var current = await http.AuthenticateAsync();
        await http.SignInAsync(principal, current.Properties);
    }

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var subject = principal?.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (principal == null || subject == null)
        {
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IdPMongoContext>();
        var user = await users.Users.Find(u => u.Id == subject).FirstOrDefaultAsync();
        var stamp = principal.FindFirst(ClaimType)?.Value;

        if (user == null || !IdPProfileService.IsActive(user) || (stamp != null && !string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal)))
        {
            // Reject and drop the cookie by hand: SignOutAsync here would authenticate again, through this very check.
            context.RejectPrincipal();
            context.Options.CookieManager.DeleteCookie(context.HttpContext, context.Options.Cookie.Name!, context.Options.Cookie.Build(context.HttpContext));
        }
    }
}
