[[go back to index]](../index.md#documentation-structure)

# Token Endpoint

The token endpoint issues tokens to clients. It supports the `authorization_code`, `client_credentials`,
`refresh_token` and `urn:ietf:params:oauth:grant-type:device_code` grant types, and
[extension grants](../topics/extension_grants.md) registered with `AddExtensionGrantValidator<T>()`. The `password`
grant is deprecated by OAuth 2.1: it works only for clients with `Client.AllowResourceOwnerPasswordFlow` and an
`IResourceOwnerPasswordValidator` registered with `AddResourceOwnerValidator<T>()`.

| Route | Parameters |
|---|---|
| `POST /connect/token` | In an `application/x-www-form-urlencoded` body. Any other content type gets `400` `invalid_request` |

With mutual TLS in path mode, the endpoint also answers at `/connect/mtls/token`, where clients authenticate with
their certificate. Any other HTTP method gets `405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableTokenEndpoint` is `false`.

The server supports a subset of the OpenID Connect and OAuth 2.0 token request parameters. For the full list, see the
[specification](https://openid.net/specs/openid-connect-core-1_0.html#TokenRequest).

## Client authentication

Confidential clients authenticate with one of:

* `client_id` and `client_secret` in an `Authorization: Basic` header (`client_secret_basic`) or in the body
  (`client_secret_post`);
* a signed JWT in `client_assertion` and `client_assertion_type`
  (`urn:ietf:params:oauth:client-assertion-type:jwt-bearer`), with `AddJwtBearerClientAuthentication()` (see
  [client authentication](../topics/client_authentication.md));
* a TLS client certificate on the mTLS endpoint, with `AddMutualTlsSecretValidators()` (see
  [mutual TLS](../topics/mtls.md)).

Public clients (`RequireClientSecret = false`) send only `client_id`.

## Parameters

`grant_type`
: `authorization_code`, `client_credentials`, `refresh_token`, `urn:ietf:params:oauth:grant-type:device_code`,
  `password` or an extension grant type (required).

`client_id`
: client identifier, in the body or in the `Authorization` header (required).

`client_secret`
: client secret, in the body or in the `Authorization` header.

`scope`
: one or more registered scopes. Without it, `client_credentials` gets all the client's allowed scopes. With
  `refresh_token` it can only narrow the scopes of the original grant.

`code`
: the authorization code (required for `authorization_code`).

`redirect_uri`
: the redirect URI of the authorize request (required for `authorization_code`).

`code_verifier`
: the PKCE code verifier (required for `authorization_code` when the client has `RequirePkce`, the default, or the
  authorize request had a `code_challenge`).

`refresh_token`
: the refresh token (required for `refresh_token`).

`device_code`
: the device code (required for `urn:ietf:params:oauth:grant-type:device_code`).

`username`, `password`
: the resource owner's credentials (required for `password`).

`resource`
: a resource indicator ([RFC 8707](https://www.rfc-editor.org/rfc/rfc8707)), at most one. It selects the audience of
  the access token. With `authorization_code` and `refresh_token` it must be one of the resources of the original
  grant (or, when it had none, an API resource of its scopes); with `client_credentials` it must be an API resource of
  the requested scopes.

The request can also carry a `DPoP` header with a proof ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)). The
server validates it and binds the access token to its key: the token carries `cnf.jkt` and the response says
`"token_type": "DPoP"`. A client with `RequireDPoP` must send one. An authorization code bound with `dpop_jkt`, and a
refresh token of a public client issued with DPoP, can only be redeemed with a proof of the same key. See
[proof of possession](../topics/pop.md#dpop).

## Examples

Client credentials, with the secret in the `Authorization` header:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic d29ya2VyOndvcmtlci1zZWNyZXQ=

grant_type=client_credentials&scope=orders.read
```

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store, no-cache, max-age=0
Pragma: no-cache

{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…",
  "expires_in": 3600,
  "token_type": "Bearer",
  "scope": "orders.read"
}
```

Authorization code with PKCE, redeemed by a client that authenticates in the body:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id=web
&client_secret=web-secret
&code=SplxlOBeZQQYbYS6WxSbIA
&redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc
&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
```

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store, no-cache, max-age=0
Pragma: no-cache

{
  "id_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…",
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…",
  "expires_in": 3600,
  "token_type": "Bearer",
  "refresh_token": "8xLOxBtZp8",
  "scope": "openid profile orders.read offline_access"
}
```

The response has a `refresh_token` when the client has `AllowOfflineAccess` and asked for `offline_access`.

## Errors

Errors are `400 Bad Request` with a JSON body, including client authentication failures (`invalid_client`) when the
credentials were in the body. A client that sent them in the `Authorization` header gets `401 Unauthorized` with
`WWW-Authenticate: Basic realm="OidcServer"` instead, as RFC 6749 section 5.2 requires:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": "invalid_grant"
}
```

Besides the OAuth errors (`invalid_request`, `invalid_client`, `invalid_grant`, `unauthorized_client`,
`unsupported_grant_type`, `invalid_scope`, `invalid_target`), the device code grant returns `authorization_pending`,
`slow_down`, `access_denied` and `expired_token`, and DPoP failures return `invalid_dpop_proof`. A
[custom token request validator](../topics/custom_token_request_validation.md) can add more fields to the response.

## Clients

[MinApiLib.Oidc.Http](../../src/MinApiLib.Oidc.Http/README.md) gets client credentials tokens for an `HttpClient`,
as bearer or DPoP-bound tokens, and renews them before they expire (`AddClientCredentialsTokens`). In ASP.NET Core,
the OpenID Connect handler redeems the authorization code, and
[MinApiLib.Oidc.Authentication](../../src/MinApiLib.Oidc.Authentication/README.md) refreshes the user's tokens. The
[worker and web client samples](../../samples/README.md) use them.

[[go back to index]](../index.md#documentation-structure)
