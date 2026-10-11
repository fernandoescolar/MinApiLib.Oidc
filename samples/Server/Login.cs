namespace Samples.Server;

/// <summary>
/// Signs the user in to the server once a password or a passkey has been checked, and says how: <c>amr</c>
/// (RFC 8176) and <c>acr</c> go into the tokens the clients get.
/// </summary>
public static class Login
{
    /// <summary>
    /// The <c>acr</c> of phishing-resistant authentication (OpenID Connect Extended Authentication Profile): a
    /// passkey. A client asks for it with <c>acr_values=phr</c>, and the login page then only offers passkeys.
    /// </summary>
    public const string PhishingResistant = "phr";

    public static bool RequiresPasskey(AuthorizationRequest? context) => context?.AcrValues.Contains(PhishingResistant) == true;

    public static Task SignInWithPasswordAsync(HttpContext http, SampleUser user)
        => SignInAsync(http, user, ["pwd"], acr: null);

    /// <summary>
    /// A passkey is a key (<c>hwk</c>, or <c>swk</c> when it is synced between devices) and, when the authenticator
    /// verified the user with a PIN or biometrics, a second factor (<c>mfa</c>); otherwise only user presence
    /// (<c>user</c>).
    /// </summary>
    public static Task SignInWithPasskeyAsync(HttpContext http, SampleUser user, UserPasskeyInfo passkey)
        => SignInAsync(http, user, [passkey.IsBackedUp ? "swk" : "hwk", passkey.IsUserVerified ? "mfa" : "user"], PhishingResistant);

    /// <summary>
    /// Back to the authorize request, or to a page of this server. Anything else could be an open redirect.
    /// </summary>
    public static IResult Return(string? returnUrl, AuthorizationRequest? context)
        => context != null || IsLocal(returnUrl) ? Results.Redirect(returnUrl!) : Results.Redirect("/");

    public static bool IsLocal(string? url)
        => url is { Length: > 0 } && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

    private static Task SignInAsync(HttpContext http, SampleUser user, string[] amr, string? acr)
    {
        var principal = new OidcServerUser(user.Id)
        {
            DisplayName = user.Name,
            AuthenticationMethods = amr,
            AdditionalClaims = { new Claim("name", user.Name), new Claim("email", user.Email) }
        };

        if (acr != null)
        {
            principal.AdditionalClaims.Add(new Claim("acr", acr));
        }

        return http.SignInAsync(principal);
    }
}
