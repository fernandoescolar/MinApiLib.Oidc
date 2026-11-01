[[go back to index]](../index.md#documentation-structure)

# End Session Endpoint

The end session endpoint starts single sign-out (see
[OpenID Connect RP-Initiated Logout](https://openid.net/specs/openid-connect-rpinitiated-1_0.html)). A client
redirects the user's browser to it. All the applications the user signed in to during the session can take part in
the sign-out, through front-channel and back-channel logout (see [sign-out](../topics/signout.md)).

| Route | Parameters |
|---|---|
| `GET /connect/endsession` | In the query |
| `POST /connect/endsession` | In an `application/x-www-form-urlencoded` body |
| `GET /connect/endsession/callback` | Renders the front-channel logout iframes. The logout page loads it from `LogoutRequest.SignOutIFrameUrl`; clients do not call it |

With mutual TLS in path mode, the same routes also answer under `/connect/mtls/endsession`. Any other HTTP method gets
`405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableEndSessionEndpoint` is `false`. Its URL is in the [discovery document](discovery.md)
as `end_session_endpoint`.

The endpoint validates the parameters, stores them and redirects the browser to the logout page,
`UserInteraction.LogoutUrl` (`/account/logout` by default), with a `logoutId` parameter. The host writes that page: it
reads the request with `IOidcServerInteractionService.GetLogoutContextAsync(logoutId)`, signs the user out and
redirects to `PostLogoutRedirectUri` if there is one. [samples/Server](../../samples/Server/Pages/LogoutPage.cs) has a
minimal one.

## Parameters

`id_token_hint`
: the identity token the client received. It identifies the client and the user. When it is valid and belongs to the
  current user, `LogoutRequest.ShowSignoutPrompt` is `false`: the logout page can sign the user out without asking.

`client_id`
: the client identifier. Without `id_token_hint`, it identifies the client so the server can validate
  `post_logout_redirect_uri`. It does not authenticate the request, so the logout page should still ask the user to
  confirm (`ShowSignoutPrompt` stays `true`). With `id_token_hint`, it must be the client of the token.

`post_logout_redirect_uri`
: where to send the user after the sign-out. It is accepted only when the client is known (from `id_token_hint` or
  `client_id`) and the value is one of the client's `PostLogoutRedirectUris` (see [client](../reference/client.md)).
  Otherwise it is ignored.

`state`
: returned to the client as a query string parameter of `post_logout_redirect_uri`. Ignored without a valid
  `post_logout_redirect_uri`.

## Example

```http
GET /connect/endsession?id_token_hint=eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
    &post_logout_redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignout-callback-oidc
    &state=CfDJ8Hw7 HTTP/1.1
Host: login.example.com
```

```http
HTTP/1.1 302 Found
Location: https://login.example.com/account/logout?logoutId=CfDJ8MjQ…
```

An invalid request (for example an `id_token_hint` that does not validate, or that belongs to another user) is not
an error for the browser: the user still reaches the logout page, without the client's parameters. After a `POST`
request the redirect is a `303 See Other`.

## Clients

Microsoft's OpenID Connect handler sends this request when the application signs out of its OpenID Connect scheme
(`SignOutAsync`), with `id_token_hint` when it saved the tokens.

[[go back to index]](../index.md#documentation-structure)
