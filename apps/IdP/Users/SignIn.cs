namespace IdP.Users;

/// <summary>
/// Signs a user in to the server once a credential has been checked, and says how: <c>amr</c> (RFC 8176), <c>acr</c>
/// and <c>idp</c> go into the tokens the clients get. Also decides where to go afterwards.
/// </summary>
public static class SignIn
{
    /// <summary>The <c>acr</c> of a phishing-resistant sign-in (a passkey). A client asks for it with <c>acr_values=phr</c>.</summary>
    public const string PhishingResistant = "phr";

    /// <summary>The <c>acr</c> of a sign-in with a second factor. A client asks for it with <c>acr_values=mfa</c>.</summary>
    public const string MultiFactor = "mfa";

    public const string LocalIdentityProvider = "local";

    public static bool RequiresPasskey(AuthorizationRequest? context) => context?.AcrValues.Contains(PhishingResistant) == true;

    public static bool RequiresMfa(AuthorizationRequest? context) => context?.AcrValues.Contains(MultiFactor) == true;

    public static Task WithPasswordAsync(HttpContext http, IdPUser user, IdPMongoContext context, IEventService events)
        => SignInAsync(http, user, ["pwd"], acr: null, LocalIdentityProvider, context, events);

    public static Task WithPasswordAndTotpAsync(HttpContext http, IdPUser user, IdPMongoContext context, IEventService events)
        => SignInAsync(http, user, ["pwd", "otp", "mfa"], MultiFactor, LocalIdentityProvider, context, events);

    /// <summary>
    /// A passkey is a key (<c>hwk</c>, or <c>swk</c> when it is synced between devices) and, when the authenticator verified
    /// the user with a PIN or biometrics, a second factor (<c>mfa</c>); otherwise only user presence (<c>user</c>).
    /// </summary>
    public static Task WithPasskeyAsync(HttpContext http, IdPUser user, UserPasskeyInfo passkey, IdPMongoContext context, IEventService events)
        => SignInAsync(http, user, [passkey.IsBackedUp ? "swk" : "hwk", passkey.IsUserVerified ? "mfa" : "user"], PhishingResistant, LocalIdentityProvider, context, events);

    public static Task WithExternalProviderAsync(HttpContext http, IdPUser user, string provider, IdPMongoContext context, IEventService events)
        => SignInAsync(http, user, ["external"], acr: null, provider, context, events);

    /// <summary>Back to the authorize request, or to a page of this server. Anything else could be an open redirect.</summary>
    public static IResult Return(string? returnUrl, AuthorizationRequest? context)
        => context != null || IsLocal(returnUrl) ? Results.Redirect(returnUrl!) : Results.Redirect("/account");

    public static bool IsLocal(string? url)
        => url is { Length: > 0 } && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

    private static async Task SignInAsync(HttpContext http, IdPUser user, string[] amr, string? acr, string idp, IdPMongoContext context, IEventService events)
    {
        var principal = new OidcServerUser(user.Id)
        {
            DisplayName = user.DisplayName,
            IdentityProvider = idp,
            AuthenticationMethods = amr,
            AuthenticationTime = DateTime.UtcNow,
            AdditionalClaims =
            {
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),
                new Claim("security_stamp", user.SecurityStamp)
            }
        };

        if (user.Email != null)
        {
            principal.AdditionalClaims.Add(new Claim(JwtClaimTypes.Email, user.Email));
        }

        if (acr != null)
        {
            principal.AdditionalClaims.Add(new Claim(JwtClaimTypes.AuthenticationContextClassReference, acr));
        }

        foreach (var role in user.Roles)
        {
            principal.AdditionalClaims.Add(new Claim(JwtClaimTypes.Role, role));
        }

        if (user.Roles.Count > 0 || user.Groups.Count > 0)
        {
            // Roles granted through groups also count for the consoles' admin check.
            var groups = user.Groups.Count == 0 ? [] : await context.Groups.Find(g => user.Groups.Contains(g.Id)).ToListAsync();
            foreach (var role in groups.SelectMany(g => g.Roles).Except(user.Roles, StringComparer.OrdinalIgnoreCase))
            {
                principal.AdditionalClaims.Add(new Claim(JwtClaimTypes.Role, role));
            }
        }

        await http.SignInAsync(principal);
        await context.Users.UpdateOneAsync(u => u.Id == user.Id, Builders<IdPUser>.Update.Set(u => u.LastSignInAt, DateTime.UtcNow));
        await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.DisplayName, interactive: true));
    }
}
