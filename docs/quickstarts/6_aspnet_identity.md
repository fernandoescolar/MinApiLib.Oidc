[[go back to index]](../index.md#documentation-structure)

# Using ASP.NET Core Identity and passkeys

#### NOTE
For the prerequisites, see the [overview](0_overview.md) first.

The server works with any user store: the host checks the credentials on its login page and tells the server who
signed in. ASP.NET Core Identity is a good choice for a new user database. It hashes passwords, handles lockout and,
since .NET 10, passkeys (WebAuthn).

MinApiLib.Oidc has no ASP.NET Core Identity integration package (the IdentityServer `AddAspNetIdentity<TUser>()` was
not ported). None is needed; you write three small pieces:

1. the login page checks the user with `UserManager<TUser>` and signs the user in to the server's session, with the
   user's id as subject id;
2. a profile service reads the user's claims from `UserManager<TUser>` for the tokens;
3. optionally, passkeys with `IPasskeyHandler<TUser>`.

[samples/Server](../../samples/Server) does all three: its users live in ASP.NET Core Identity and sign in with a
password or a passkey. This quickstart walks through its code; see the sample for the complete version.

#### NOTE
This quickstart assumes you know how ASP.NET Core Identity works. If you don't, read
[its documentation](https://learn.microsoft.com/aspnet/core/security/authentication/identity) first.

## Register ASP.NET Core Identity

The server keeps its own session cookie, so it needs only Identity's core services (`UserManager<TUser>`, password
hashing, validation), not its cookies or `SignInManager`:

```csharp
builder.Services.AddOidcServer(options => options.KeyManagement.Enabled = true)
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddProfileService<UserProfileService>();

builder.Services.AddIdentityCore<SampleUser>()
    .AddUserStore<SampleUserStore>();
```

The sample's [SampleUserStore](../../samples/Server/Users/SampleUserStore.cs) keeps two users and their passkeys in
memory and implements `IUserPasswordStore<TUser>` and `IUserPasskeyStore<TUser>`. A real server uses a database:
the Entity Framework Core stores of ASP.NET Core Identity (`AddEntityFrameworkStores<TContext>()`) or its own
implementation of the store interfaces.

## Sign the user in

The login page receives the authorize request in `returnUrl` (see [Sign-in](../topics/signin.md)). It finds the
user, checks the password with `UserManager`, signs the user in to the server with `OidcServerUser` and returns to
the authorize request. This is the sample's [LoginPage.cs](../../samples/Server/Pages/LoginPage.cs), shortened:

```csharp
var context = returnUrl == null ? null : await interaction.GetAuthorizationContextAsync(returnUrl);

var user = username == null ? null : await users.FindByNameAsync(username);
if (user == null || password == null || !await users.CheckPasswordAsync(user, password))
{
    return LoginPage.Render(http, antiforgery, returnUrl, context, "Invalid user name or password.");
}

await http.SignInAsync(new OidcServerUser(user.Id)
{
    DisplayName = user.Name,
    AuthenticationMethods = ["pwd"]
});

return context != null ? Results.Redirect(returnUrl!) : Results.Redirect("/");
```

The subject id is the Identity user id (`UserManager.GetUserIdAsync`). It must never change, because clients and
APIs identify the user by the `sub` claim. `AuthenticationMethods` becomes the `amr` claim (RFC 8176): `pwd` for a
password.

`SignInManager.PasswordSignInAsync` would sign the user in to Identity's own cookie (`Identity.Application`), which
the server does not read. To get Identity's lockout handling, register `SignInManager` with `AddSignInManager()` and
use `CheckPasswordSignInAsync`, which checks the password and counts failures without signing in; then sign in with
`OidcServerUser` as above.

## Issue the user's claims in tokens

The server asks the [profile service](../reference/profileservice.md) (`IProfileService`, namespace
`MinApiLib.Oidc.Server.Core.Users`) for the claims of the identity token, the userinfo endpoint and the access token,
and whether the user is still active. The default one copies the requested claims from the session cookie: the
sample puts `name` and `email` in the cookie (`OidcServerUser.AdditionalClaims`) and relies on it. Reading the user
from `UserManager` instead keeps the tokens up to date, also for refresh tokens issued long after the sign-in, and
stops issuing tokens to deleted or locked-out users:

```csharp
public sealed class UserProfileService(UserManager<SampleUser> users) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var user = await users.FindByIdAsync(context.Subject.GetSubjectId());
        if (user == null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Name, user.Name),
            new(JwtClaimTypes.Email, user.Email)
        };

        if (users.SupportsUserClaim)
        {
            claims.AddRange(await users.GetClaimsAsync(user));
        }

        // only the claims the client asked for, through its scopes and resources
        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var user = await users.FindByIdAsync(context.Subject.GetSubjectId());
        context.IsActive = user != null && !(users.SupportsUserLockout && await users.IsLockedOutAsync(user));
    }
}
```

`GetSubjectId` is in `MinApiLib.Oidc.Server.Core.Users` and `JwtClaimTypes` in `MinApiLib.Oidc.Server.Core.IdentityModel`.
The protocol claims of the session (`sub`, `auth_time`, `idp`, `amr` and `acr`) reach the tokens without the profile
service.

## Passkeys

Since .NET 10, ASP.NET Core Identity has `IPasskeyHandler<TUser>`. It makes the WebAuthn options for the browser and
checks the browser's answers: signature, challenge, origin and signature counter. The store keeps the passkeys
(`IUserPasskeyStore<TUser>`). The sample registers the handler and its options:

```csharp
builder.Services.TryAddScoped<IPasskeyHandler<SampleUser>, PasskeyHandler<SampleUser>>();
builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    // discoverable passkeys, so the user signs in without typing a user name
    options.ResidentKeyRequirement = "required";
    options.UserVerificationRequirement = "preferred";
});
```

(The sample also sets `ValidateOrigin` to accept its `https://localhost:5001` origin; see its
[Program.cs](../../samples/Server/Program.cs).)

A passkey sign-in is two requests from the login page:

1. The page asks the server for the options (`IPasskeyHandler.MakeRequestOptionsAsync`). The server keeps the
   ceremony state, with its challenge, until the answer arrives; the sample keeps it in an encrypted cookie for five
   minutes ([PasskeyState.cs](../../samples/Server/Passkeys/PasskeyState.cs)). The browser asks the authenticator
   with `navigator.credentials.get` ([PasskeyScripts.cs](../../samples/Server/Passkeys/PasskeyScripts.cs)).
2. The page posts the browser's answer. The server checks it and signs the user in
   ([PasskeyEndpoints.cs](../../samples/Server/Pages/PasskeyEndpoints.cs)):

```csharp
var result = await passkeys.PerformAssertionAsync(new PasskeyAssertionContext
{
    HttpContext = http,
    CredentialJson = credential,
    AssertionState = state
});

if (!result.Succeeded)
{
    return LoginPage.Render(http, antiforgery, returnUrl, context, $"The passkey was not accepted: {result.Failure.Message}");
}

// keep the new signature counter, which reveals cloned authenticators
await users.AddOrUpdatePasskeyAsync(result.User, result.Passkey);
await Login.SignInWithPasskeyAsync(http, result.User, result.Passkey);
```

Registering a passkey is the same with `MakeCreationOptionsAsync`, `navigator.credentials.create` and
`PerformAttestationAsync`, on a page for the signed-in user ([PasskeysPage.cs](../../samples/Server/Pages/PasskeysPage.cs)).

### How the user signed in: amr and acr

[Login.cs](../../samples/Server/Login.cs) tells the clients how the user signed in, with the `amr` and `acr` claims:

```csharp
// a key on this device (hwk) or a synced one (swk); the authenticator verified the user (mfa) or not (user)
var amr = new[] { passkey.IsBackedUp ? "swk" : "hwk", passkey.IsUserVerified ? "mfa" : "user" };

var principal = new OidcServerUser(user.Id)
{
    DisplayName = user.Name,
    AuthenticationMethods = amr,
    AdditionalClaims = { new Claim("acr", "phr") }   // phishing-resistant
};

await http.SignInAsync(principal);
```

`phr` is the phishing-resistant authentication value of the OpenID Connect Extended Authentication Profile. A
password sign-in gets `amr` = `pwd` and no `acr`.

## Step-up with acr_values

A client can ask for a phishing-resistant sign-in with `acr_values=phr`. The login page reads the request from the
authorization context and, in the sample, offers only the passkey:

```csharp
var context = await interaction.GetAuthorizationContextAsync(returnUrl);
var requiresPasskey = context?.AcrValues.Contains("phr") == true;
```

By default, as in OpenID Connect Core, `acr_values` is a voluntary request: it only reaches the login page, and a
user who already has a session gets tokens whatever the values ask for. The sample turns on enforcement:

```csharp
builder.Services.AddOidcServer(options => options.Authentication.EnforceAcrValues = true);
```

With `EnforceAcrValues`, the authorize endpoint requires the session's `acr` claim to be one of the request's
`acr_values`. A user who signed in with a password and then reaches a client that asks for `phr` is sent to the login
page once, to sign in again with a passkey (step-up). If the session still does not meet the values when the user
comes back, the client gets the `unmet_authentication_requirements` error at its redirect URI. The `idp:` and
`tenant:` values are not checked. Turn it on only when the login page sets the `acr` claim of the sessions it creates.

On the client side, the ASP.NET Core OpenID Connect handler removes `acr` from the user's claims by default; the
sample web client keeps it with `options.ClaimActions.Remove("acr")`.

## Try it

Run the samples as their [README](../../samples/README.md#passkeys) describes: sign in as `bob` / `bob` on
https://localhost:5001/account/passkeys, add a passkey, then use **Sign in with a passkey** on the web client. Its home
page shows `amr` and `acr` from the identity token.

## What's missing

The sample has no registration, password reset, email confirmation or account management pages, and keeps the users
in memory. ASP.NET Core Identity provides the building blocks for those (`UserManager<TUser>` creates users, resets
passwords and generates confirmation tokens); you write the pages, as you do the login page.

[[go back to index]](../index.md#documentation-structure)
