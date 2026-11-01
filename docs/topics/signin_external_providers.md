[[go back to index]](../index.md#documentation-structure)

# Sign-in with External Identity Providers

The login page can let users sign in with another identity provider: Microsoft Entra ID, Google, another OpenID
Connect server, a SAML provider... The server does not need to know about them. They are ordinary ASP.NET Core
authentication handlers, and the login page turns the external identity into a session at the server. It takes a few
steps.

#### NOTE
ASP.NET Core Identity can hide some of these steps (`SignInManager.GetExternalLoginInfoAsync` and the external login
stores). Microsoft's [documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/) covers
that path. This page shows the steps without it.

## Adding authentication handlers for external providers

The protocol used to talk to an external provider is implemented by an *authentication handler*. Some providers use
proprietary protocols (social providers such as Facebook) and others use standard ones: OpenID Connect,
WS-Federation or SAML2p. Microsoft ships handlers for the common ones (for example
`Microsoft.AspNetCore.Authentication.Google` and `Microsoft.AspNetCore.Authentication.OpenIdConnect`).

## The role of cookies

The `SignInScheme` option of an external handler names the cookie handler that keeps the result of the external
sign-in, the claims the provider sent, for the few redirects until the login page has dealt with it.

The server registers a cookie handler for this: `OidcServerConstants.ExternalCookieAuthenticationScheme`
(`idsrv.external`). Use it as the `SignInScheme`:

```csharp
builder.Services.AddOidcServer()
    // ...
    ;

builder.Services.AddAuthentication()
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = OidcServerConstants.ExternalCookieAuthenticationScheme;

        options.ClientId = "...";
        options.ClientSecret = "...";
    });
```

Or register your own cookie handler:

```csharp
builder.Services.AddAuthentication()
    .AddCookie("external")
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = "external";

        options.ClientId = "...";
        options.ClientSecret = "...";
    });
```

#### NOTE
In special cases you can skip the external cookie and sign the external user in to the session cookie directly. That
usually means handling the events of the external handler to transform the claims of the external identity.

## Triggering the authentication handler

The login page starts the external sign-in with a challenge to the provider's scheme. It passes the URL of its
callback, and keeps the provider and the return URL in the authentication properties:

```csharp
app.MapGet("/account/external", (string provider, string? returnUrl) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/account/external/callback",
        Items =
        {
            ["scheme"] = provider,
            ["returnUrl"] = returnUrl ?? "/"
        }
    };

    return Results.Challenge(properties, [provider]);
});
```

`IAuthenticationSchemeProvider.GetAllSchemesAsync()` lists the registered handlers, so the login page can show a
button for each one. `AuthorizationRequest.IdP` (the `idp:` value of `acr_values`) and
`Client.IdentityProviderRestrictions` tell it which ones a request allows (see the
[federation gateway](federation_gateway.md)).

## Handling the callback and signing in the user

On the callback, the page usually:

* reads the identity the external provider returned;
* decides what to do with the user, which can differ for new and returning users;
* for new users, maybe asks for more information before letting them in;
* finds the local user account linked to the external identity, or creates one;
* keeps the external claims it needs;
* deletes the temporary cookie;
* signs the user in to the server and returns to the authorize request.

```csharp
app.MapGet("/account/external/callback", async (
    HttpContext http,
    IOidcServerInteractionService interaction,
    UserStore users) =>
{
    // the outcome of the external sign-in, from the temporary cookie
    var result = await http.AuthenticateAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);
    if (result?.Succeeded != true)
    {
        return Results.BadRequest("External authentication error");
    }

    // the unique id of the external user: usually sub or NameIdentifier, depending on the provider
    var externalUser = result.Principal;
    var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject)
        ?? externalUser.FindFirst(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Unknown user id");

    var provider = result.Properties.Items["scheme"]!;
    var returnUrl = result.Properties.Items["returnUrl"]!;

    // your own code: find the user by provider and external id, or provision a new one
    var user = await users.FindOrCreateExternalAsync(provider, userIdClaim.Value, externalUser.Claims);

    // the session at the server
    await http.SignInAsync(new OidcServerUser(user.Id)
    {
        DisplayName = user.Name,
        IdentityProvider = provider
    });

    // delete the temporary cookie
    await http.SignOutAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);

    // back to the authorize request, or to a local page: anything else could be an open redirect
    return interaction.IsValidReturnUrl(returnUrl) || IsLocal(returnUrl)
        ? Results.Redirect(returnUrl)
        : Results.Redirect("/");
});
```

`IsLocal` accepts paths such as `/account/passkeys` and rejects `//host` and `/\host`; the sample server's
[Login.cs](../../samples/Server/Login.cs) has one.

`JwtClaimTypes` is in the `MinApiLib.Oidc.Server.Core.IdentityModel` namespace. The `idp` claim set by
`IdentityProvider` matters at sign-out: the logout page reads it to sign the user out of the external provider too
(see [Sign-out of External Identity Providers](signout_external_providers.md)). Without `AuthenticationMethods`,
`amr` is `external`. See [Sign-in](signin.md) for the other claims.

## State, URL length, and ISecureDataFormat

The client's state often has to make the round trip to the external provider: it is captured before the redirect
and given back when the user returns. Many protocols, OpenID Connect included, have a request parameter for it,
which the provider returns in its response. The ASP.NET Core OpenID Connect handler uses it, with the authentication
properties above.

That parameter can make the URL too long (over the common limit of 2000 characters). The OpenID Connect handler can
keep the state on the server instead, with an `ISecureDataFormat<AuthenticationProperties>` in
`OpenIdConnectOptions.StateDataFormat`.

The server provides one, `DistributedCacheStateDataFormatter`, backed by the registered `IDistributedCache`
(`AddOidcServer()` registers the in-memory one; use a shared cache when the server runs on several instances).
`AddOidcStateDataFormatterCache` sets it on the OpenID Connect handlers. Without arguments it applies to all of them:

```csharp
// keeps the state parameter of the OpenID Connect handlers in the IDistributedCache
builder.Services.AddOidcStateDataFormatterCache();

builder.Services.AddAuthentication()
    .AddOpenIdConnect("entra", "Microsoft Entra ID", options =>
    {
        // ...
    })
    .AddOpenIdConnect("adfs", "ADFS", options =>
    {
        // ...
    });
```

To apply it to some handlers only, pass their schemes:

```csharp
builder.Services.AddOidcStateDataFormatterCache("entra");
```

[[go back to index]](../index.md#documentation-structure)
