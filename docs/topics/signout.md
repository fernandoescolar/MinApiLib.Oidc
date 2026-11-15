[[go back to index]](../index.md#documentation-structure)

# Sign-out

Signing out of the server means removing its session cookie. A complete sign-out also signs the user out of the
client applications of the session, and maybe of an upstream identity provider.

## Removing the authentication cookie

Call `SignOutAsync` on the `HttpContext`, with the session cookie scheme (`OidcServerConstants.DefaultCookieAuthenticationScheme`,
`idsrv`, unless you changed it) or with no scheme, which uses the default one:

```csharp
await http.SignOutAsync(OidcServerConstants.DefaultCookieAuthenticationScheme);
```

```csharp
await http.SignOutAsync();
```

#### NOTE
Usually the logout page should ask the user to confirm the sign-out (a POST with an antiforgery token). Otherwise
another site could link to it and sign the user out. `LogoutRequest.ShowSignoutPrompt` (below) says when the
confirmation can be skipped.

## Notifying clients that the user has signed out

The server supports three ways to tell clients that the user signed out:
[front-channel logout](https://openid.net/specs/openid-connect-frontchannel-1_0.html) and
[back-channel logout](https://openid.net/specs/openid-connect-backchannel-1_0.html) for server-side web
applications, and [session management](https://openid.net/specs/openid-connect-session-1_0.html) for browser-based
applications.

**Front-channel**

The "signed out" page must render an `<iframe>` that loads the front-channel logout URL of each client of the
session. Clients that want it set `Client.FrontChannelLogoutUri`. The server tracks the clients the user signed in
to. `IOidcServerInteractionService.GetLogoutContextAsync(logoutId)` returns a `LogoutRequest` whose
`SignOutIFrameUrl` is the URL of that iframe (see the [interaction service](../reference/interactionservice.md)).
It is `null` when no client needs it.

**Back-channel**

When the logout page removes the session cookie with `SignOutAsync`, the server sends a logout token to the
`Client.BackChannelLogoutUri` of each client of the session, through `IBackChannelLogoutService`. By default it
sends them in the background (`Authentication.SendBackChannelLogoutNotificationsInBackground`), so the page does not
wait for the clients.

**Browser-based applications**

Session management needs nothing special from the logout page. The client watches the server's
`check_session_iframe` (the check session endpoint, which reads the `idsrv.session` cookie) and learns that the
session changed. Browser libraries such as the third-party [oidc-client-ts](https://github.com/authts/oidc-client-ts)
implement the monitoring (its `monitorSession` setting). Browsers that block third-party cookies break this
mechanism; a [backend for frontend](../quickstarts/4_javascript_client.md) with back-channel logout does not depend
on it.

## Sign-out initiated by a client application

A client starts the sign-out by redirecting the user to the [end session endpoint](../endpoints/endsession.md). The
endpoint keeps what the logout page needs, such as the client's post-logout redirect URI, as temporary state, and
redirects to `UserInteraction.LogoutUrl` with its identifier in the `UserInteraction.LogoutIdParameter` query
parameter (`logoutId`). `LogoutUrl` defaults to `/account/logout`.

`GetLogoutContextAsync(logoutId)` loads that state. `LogoutRequest.ShowSignoutPrompt` says whether the request is
authenticated, so the page can skip the confirmation. Only a valid `id_token_hint` authenticates the request. A
client can send just its `client_id` instead, so that its `post_logout_redirect_uri` can be checked against the
registered URIs, but anyone can forge that request and the prompt is still shown.

The logout page of [samples/Server](../../samples/Server/Pages/LogoutPage.cs) signs the user out and hands the
front-channel iframe URL and the way back to the client to its React page
([LogoutPage.tsx](../../samples/Server/ClientApp/src/pages/LogoutPage.tsx)), which renders them:

```csharp
var logout = await interaction.GetLogoutContextAsync(logoutId);
await http.SignOutAsync();

return spa.Render("logout", "Signed out", new
{
    postLogoutRedirectUri = logout.PostLogoutRedirectUri,
    clientName = logout.ClientName ?? logout.ClientId,
    signOutIFrameUrl = logout.SignOutIFrameUrl
});
```

```tsx
{model.signOutIFrameUrl && <iframe src={model.signOutIFrameUrl} title="Front-channel logout" className="hidden" />}
```

(The sample signs out without a confirmation, to stay short.)

By default the state travels protected in the `logoutId` value itself. To keep it elsewhere between the end session
endpoint and the logout page, implement `IMessageStore<LogoutMessage>` and register it in the container.

[[go back to index]](../index.md#documentation-structure)
