[[go back to index]](../index.md#documentation-structure)

# Federated Sign-out

Federated sign-out is the case where a user signed in to the server with an external identity provider, and then
signs out of that provider by a path the server does not know about. The server should hear about it, so it can sign
the user out of the server and of every client application of the session.

Not every external provider supports federated sign-out. Those that do notify their clients that the user signed
out, usually with a request in an `<iframe>` of the provider's "signed out" page. The server must then notify its own
clients, [as in a normal sign-out](signout.md), also with requests in an `<iframe>`, inside the provider's one.

What makes federated sign-out special, compared to a normal [sign-out](signout.md), is that the request does not
arrive at the server's end session endpoint. Each external provider calls an endpoint of its own authentication
handler in the server host, because each handler speaks a different protocol and listens on different paths (for
example the `/signout-oidc` path of the OpenID Connect handler).

So no "signed out" page of the host runs, and nothing renders the notifications to the server's clients. The server
adds them itself: `AddOidcServer()` wraps the authentication handlers (`FederatedSignoutAuthenticationHandlerProvider`).
When a handler processes a sign-out request from its provider and signs the user out of the server's session cookie,
the server writes to the response the same front-channel logout `<iframe>` that the logout page would render.

The handler must sign out of the server's session, so set its sign-out scheme to the session cookie scheme
(`OidcServerConstants.SignoutScheme`, which is `idsrv`):

```csharp
builder.Services.AddAuthentication()
    .AddOpenIdConnect("entra", "Microsoft Entra ID", options =>
    {
        options.SignInScheme = OidcServerConstants.ExternalCookieAuthenticationScheme;
        options.SignOutScheme = OidcServerConstants.SignoutScheme;
        // ...
    });
```

With that, federated sign-out works without more code.

[[go back to index]](../index.md#documentation-structure)
