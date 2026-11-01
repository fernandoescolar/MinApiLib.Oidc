[[go back to index]](../index.md#documentation-structure)

# Authorize Endpoint

The authorize endpoint is where the browser goes to sign the user in and get an authorization code (or, for clients
that allow it, tokens). It usually involves the login page and, optionally, the consent page.

| Route | Parameters |
|---|---|
| `GET /connect/authorize` | In the query |
| `POST /connect/authorize` | In an `application/x-www-form-urlencoded` body. Any other content type gets `415` |
| `GET /connect/authorize/callback` | The return URL the server hands to the login and consent pages. Clients do not call it |

With mutual TLS in path mode, the same routes also answer under `/connect/mtls/authorize`. Any other HTTP method gets
`405` with an `Allow` header. The endpoint answers `404` when `OidcServerOptions.Endpoints.EnableAuthorizeEndpoint`
is `false`.

The server supports a subset of the OpenID Connect and OAuth 2.0 authorize request parameters. For the full list,
see the [specification](https://openid.net/specs/openid-connect-core-1_0.html#AuthRequest).

## Parameters

`client_id`
: identifier of the client (required).

`scope`
: one or more registered scopes, separated by spaces (required).

`redirect_uri`
: must exactly match one of the client's `RedirectUris` (required). It must not contain a fragment.

`response_type`
: `code` requests an authorization code. This is the flow to use.
  <br/>
  `code id_token` requests an authorization code and an identity token (hybrid flow).
  <br/>
  `id_token`, `id_token token`, `token`, `code token` and `code id_token token` are supported for existing clients.
  The implicit flow needs `Client.AllowImplicitFlow`, and any response type with `token` needs
  `Client.AllowAccessTokensViaBrowser`. Both are off by default.

`response_mode`
: `query` (the default for `code`), `fragment` (the default for the other response types) or `form_post`, which
  sends the response as an auto-submitted form instead of a redirect (optional). `query` is only allowed for `code`.

`state`
: echoed back in the response, to correlate request and response and to protect against CSRF (recommended).

`nonce`
: echoed back in the identity token, for replay protection. Required when the response includes an identity token
  from the authorize endpoint (implicit and hybrid flows with the `openid` scope).

`code_challenge`
: the PKCE code challenge. Required unless the client sets `RequirePkce = false`.

`code_challenge_method`
: `S256`. `plain` is accepted only for clients with `AllowPlainTextPkce`. Without the parameter the server assumes
  `plain`, so always send `S256`.

`prompt`
: `none`: no UI is shown. If the user has to sign in or consent, the client gets an error (`login_required`,
  `consent_required`, ...).
  <br/>
  `login`: the login page is shown even if the user already has a session.
  <br/>
  `consent`: the consent page is shown even if the user already consented.
  <br/>
  `select_account`: passed to the login page.
  <br/>
  Other values (for example `create`) are accepted once registered with `AddPromptRedirect(prompt, url)` or
  `AddPromptHandler<T>(prompt)`, which add them to `UserInteraction.PromptValuesSupported` (see
  [prompt](../topics/prompt.md)).

`login_hint`
: can be used to pre-fill the user name on the login page.

`ui_locales`
: a hint about the display language of the login page.

`display`
: `page`, `popup`, `touch` or `wap`. Passed to the login page.

`max_age`
: if the user's session is older than this many seconds, the login page is shown.

`acr_values`
: authentication context references, separated by spaces. They reach the login page through the authorization
  context (`IOidcServerInteractionService.GetAuthorizationContextAsync`). The server handles these values itself:
  <br/>
  > `idp:name_of_idp` skips the login page's provider selection and sends the user to that identity provider (if
  > the client's `IdentityProviderRestrictions` allow it).
  <br/>
  > `tenant:name_of_tenant` passes a tenant name to the login page.
  <br/>
  > `urn:auth:nonce` signs the user in with a one-time nonce from the [nonce endpoint](nonce.md), sent in
  > `login_hint_token` (see the [nonce authorization pattern](../reference/nonce_pattern.md)).
  <br/>
  With `OidcServerOptions.Authentication.EnforceAcrValues` (off by default) the other values are required: the
  session's `acr` claim must be one of them. If it is not, the user is sent to the login page once, which can ask for
  a stronger sign-in (step-up). If the session still does not meet them, the client gets
  `unmet_authentication_requirements`. Without the option, the values only reach the login page.

`login_hint_token`
: the nonce of the nonce authorization pattern, with `acr_values=urn:auth:nonce`.

`resource`
: a resource indicator ([RFC 8707](https://www.rfc-editor.org/rfc/rfc8707)): the name of an API resource, as an
  absolute URI without a fragment. It can be repeated. It limits the audience of the access token to those APIs, and
  is required for API resources with `RequireResourceIndicator`. The token request can then pick one of them.

`dpop_jkt`
: the thumbprint of the client's DPoP key ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449) section 10). It binds
  the authorization code to that key: only a token request with a proof of that key can redeem it. Only for
  `response_type=code`. See [proof of possession](../topics/pop.md#dpop).

`request`
: a request object: some or all of the parameters as a signed JWT (see
  [request objects](../topics/request_object.md)).

`request_uri`
: either the `request_uri` returned by the [pushed authorization endpoint](../topics/par.md)
  (`urn:ietf:params:oauth:request_uri:...`), sent together with `client_id` only, or the URL of a request object.
  Request object URLs are off by default (`OidcServerOptions.Endpoints.EnableJwtRequestUri`) and must be registered
  in the client's `RequestUris`.

When the server or a client requires pushed authorization requests (`PushedAuthorization.Required`,
`Client.RequirePushedAuthorization`), the endpoint accepts only a `request_uri` from the pushed authorization
endpoint.

## Example

A code flow request with PKCE (line breaks added for readability):

```http
GET /connect/authorize?client_id=web
    &response_type=code
    &scope=openid%20profile%20orders.read%20offline_access
    &redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc
    &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
    &code_challenge_method=S256
    &state=af0ifjsldkj
    &nonce=n-0S6_WzA2Mj HTTP/1.1
Host: login.example.com
```

If the user has no session, the server redirects to the login page with a return URL to
`/connect/authorize/callback`. Once the user is signed in (and has consented, if needed), the server redirects back
to the client:

```http
HTTP/1.1 302 Found
Location: https://web.example.com/signin-oidc?code=SplxlOBeZQQYbYS6WxSbIA
    &scope=openid%20profile%20orders.read%20offline_access
    &state=af0ifjsldkj
    &session_state=...
    &iss=https%3A%2F%2Flogin.example.com
```

`session_state` is present while the check session endpoint is on. `iss` ([RFC 9207](https://www.rfc-editor.org/rfc/rfc9207))
is present while `OidcServerOptions.EmitIssuerIdentificationResponseParameter` is `true`, the default.

With `response_mode=form_post` the server answers with an HTML page that posts the same values to the redirect URI.
After a `POST` request the redirect is a `303 See Other`.

The errors `access_denied`, `login_required`, `consent_required`, `interaction_required`,
`account_selection_required` and `unmet_authentication_requirements` go to the client's redirect URI, with `error`,
`error_description` (when there is one), `state` and `iss`:

```http
HTTP/1.1 302 Found
Location: https://web.example.com/signin-oidc?error=login_required&state=af0ifjsldkj&iss=https%3A%2F%2Flogin.example.com
```

Any other error, such as an unknown client, a redirect URI that is not registered or an invalid parameter, goes to the
error page (`UserInteraction.ErrorUrl`, `/home/error` by default) with an `errorId` that
`IOidcServerInteractionService.GetErrorContextAsync` resolves. The host writes that page;
[samples/Server](../../samples/Server/Pages/ErrorPage.cs) has a minimal one.

## Clients

In ASP.NET Core, Microsoft's OpenID Connect handler (`AddOpenIdConnect`) builds the authorize request, with PKCE, and
handles the response. [MinApiLib.Oidc.Authentication](../../src/MinApiLib.Oidc.Authentication/README.md) builds on it:
it keeps the user's tokens, refreshes them and, with DPoP, adds `dpop_jkt` to the authorize request. The
[web client sample](../../samples/README.md) uses both.

[[go back to index]](../index.md#documentation-structure)
