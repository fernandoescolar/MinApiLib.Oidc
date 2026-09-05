[[go back to index]](../index.md#documentation-structure)

# Sign-out

Signing out of IdentityServer is as simple as removing the authentication cookie,
but for doing a complete federated sign-out, we must consider signing the user out of the client applications (and maybe even up-stream identity providers) as well.

## Removing the authentication cookie

To remove the authentication cookie, simply use the `SignOutAsync` extension method on the `HttpContext`.
You will need to pass the scheme used (which is provided by `IdentityServerConstants.DefaultCookieAuthenticationScheme` unless you have changed it):

```csharp
await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);
```

Or you can use the convenience extension method that is provided by IdentityServer:

```csharp
await HttpContext.SignOutAsync();
```

#### NOTE
Typically you should prompt the user for signout (meaning require a POST), otherwise an attacker could hotlink to your logout page causing the user to be automatically logged out.

## Notifying clients that the user has signed-out

As part of the signout process you will want to ensure client applications are informed that the user has signed out.
IdentityServer supports the [front-channel](https://openid.net/specs/openid-connect-frontchannel-1_0.html) specification for server-side clients (e.g. MVC),
the [back-channel](https://openid.net/specs/openid-connect-backchannel-1_0.html)  specification for server-side clients (e.g. MVC),
and the [session management](https://openid.net/specs/openid-connect-session-1_0.html) specification for browser-based JavaScript clients (e.g. SPA, React, Angular, etc.).

**Front-channel server-side clients**

To signout the user from the server-side client applications via the front-channel spec, the “logged out” page in IdentityServer must render an `<iframe>` to notify the clients that the user has signed out.
Clients that wish to be notified must have the `FrontChannelLogoutUri` configuration value set.
IdentityServer tracks which clients the user has signed into, and provides an API called `GetLogoutContextAsync` on the `IIdentityServerInteractionService` ([details](../reference/interactionservice.md#refinteractionservice)).
This API returns a `LogoutRequest` object with a `SignOutIFrameUrl` property that your logged out page must render into an `<iframe>`.

**Back-channel server-side clients**

To signout the user from the server-side client applications via the back-channel spec the `IBackChannelLogoutService` service can be used.
IdentityServer will automatically use this service when your logout page removes the user’s authentication cookie via a call to `HttpContext.SignOutAsync`.
Clients that wish to be notified must have the `BackChannelLogoutUri` configuration value set.

**Browser-based JavaScript clients**

Given how the [session management](https://openid.net/specs/openid-connect-session-1_0.html) specification is designed, there is nothing special in IdentityServer that you need to do to notify these clients that the user has signed out.
The clients, though, must perform monitoring on the check_session_iframe, and this is implemented by the [oidc-client JavaScript library](https://github.com/IdentityModel/oidc-client-js/).

## Sign-out initiated by a client application

If sign-out was initiated by a client application, then the client first redirected the user to the [end session endpoint](../endpoints/endsession.md#refendsession).
Processing at the end session endpoint might require some temporary state to be maintained (e.g. the client’s post logout redirect uri) across the redirect to the logout page.
This state might be of use to the logout page, and the identifier for the state is passed via a logoutId parameter to the logout page.

The `GetLogoutContextAsync` API on the [interaction service](../reference/interactionservice.md#refinteractionservice) can be used to load the state.
Of interest on the `LogoutRequest` model context class is the `ShowSignoutPrompt` which indicates if the request for sign-out has been authenticated, and therefore it’s safe to not prompt the user for sign-out.
Only a valid `id_token_hint` authenticates the request. A client may instead send just its `client_id`, so that its `post_logout_redirect_uri` can be checked against the registered URIs, but anyone can forge that request and the prompt is still shown.

By default this state is managed as a protected data structure passed via the logoutId value.
If you wish to use some other persistence between the end session endpoint and the logout page, then you can implement `IMessageStore<LogoutMessage>` and register the implementation in DI.

[[go back to index]](../index.md#documentation-structure)
