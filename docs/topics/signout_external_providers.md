[[go back to index]](../index.md#documentation-structure)

# Sign-out of External Identity Providers

When a user who signed in with an [external identity provider](signin_external_providers.md) is
[signing out](signout.md) of the server, they should usually be sent to sign out of the external provider too. Not
every provider supports it: it depends on its protocol and features.

The `idp` claim of the session tells the logout page that the user signed in with an external provider. Its value
is the authentication scheme of that provider's handler (`OidcServerUser.IdentityProvider`); local users have
`local`. `HttpContext.GetSchemeSupportsSignOutAsync(scheme)` (namespace `MinApiLib.Oidc.Server.Core.Extensions`) says
whether the handler can sign out.

Sending the user to the external provider interrupts the normal sign-out, which still has to notify the clients. So
the logout page asks the external provider to redirect the user back to it after signing out. Not every provider
supports a post-logout redirect either.

The sign-out then goes like this: remove the server's session cookie, redirect to the external provider with a
post-logout redirect back to the logout page, and keep the sign-out state there: the `logoutId` value. When there
is no `logoutId` (the user started the sign-out at the server), `IOidcServerInteractionService.CreateLogoutContextAsync()`
creates one while the session still exists. Pass the return URL in `AuthenticationProperties.RedirectUri` to the
external handler's `SignOutAsync`:

```csharp
app.MapPost("/account/logout", async (
    HttpContext http,
    [FromForm] string? logoutId,
    IOidcServerInteractionService interaction,
    IEventService events) =>
{
    var user = http.User;
    if (user.Identity?.IsAuthenticated == true)
    {
        var idp = user.GetIdentityProvider();
        var external = idp != OidcServerConstants.LocalIdentityProvider && await http.GetSchemeSupportsSignOutAsync(idp);

        // keep the list of clients to notify before the session is gone
        if (external)
        {
            logoutId ??= await interaction.CreateLogoutContextAsync();
        }

        // delete the session cookie
        await http.SignOutAsync();
        await events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetDisplayName()));

        if (external)
        {
            // the external provider signs the user out and comes back to the "signed out" page, which
            // completes the sign-out at the server
            var url = "/account/loggedout?logoutId=" + Uri.EscapeDataString(logoutId ?? "");
            return Results.SignOut(new AuthenticationProperties { RedirectUri = url }, [idp]);
        }
    }

    return Results.Redirect("/account/loggedout?logoutId=" + Uri.EscapeDataString(logoutId ?? ""));
});
```

Once the user is back from the external provider, the "signed out" page does the normal processing of the
`logoutId`: it loads the logout context, renders the front-channel logout iframe and offers the way back to the
client (see [Sign-out](signout.md)).

The form needs an antiforgery token; binding `[FromForm]` makes ASP.NET Core validate it once `UseAntiforgery()` is
in the pipeline.

[[go back to index]](../index.md#documentation-structure)
