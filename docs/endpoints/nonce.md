[[go back to index]](../index.md#documentation-structure)

# Nonce Endpoint

The nonce endpoint issues a one-time token, the nonce, for the
[nonce authorization pattern](../reference/nonce_pattern.md). A client that has an access token for the user
exchanges it for a nonce, and another application signs the same user in by sending the nonce to the
[authorize endpoint](authorize.md). The nonce is short-lived and can be used only once, so it is safe in a URL: even if
it ends up in the browser history or in server logs, it cannot be used again.

This endpoint is not part of OpenID Connect or OAuth 2.0; it comes from the IdentityServer fork.

| Route | Parameters |
|---|---|
| `POST /connect/nonce` | In an `application/x-www-form-urlencoded` body. Any other content type gets `400` `invalid_request` |

With mutual TLS in path mode, the endpoint also answers at `/connect/mtls/nonce`. Any other HTTP method gets `405`
with an `Allow` header. The endpoint answers `404` when `OidcServerOptions.Endpoints.EnableNonceEndpoint` is `false`.
Its URL is in the [discovery document](discovery.md) as `nonce_endpoint`.

The client authenticates as it would at the [token endpoint](token.md), and must allow the `urn:auth:nonce` grant
type (`GrantType.NoncePattern`).

## Parameters

`token`
: an access token issued to the same client, for a user (it must have a `sub` claim) who is still active (required).

## Example

```http
POST /connect/nonce HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic bW9iaWxlOm1vYmlsZS1zZWNyZXQ=

token=eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "nonce": "3F8B2C1D9E7A4B6C5D0E1F2A3B4C5D6E",
  "expires_in": 120
}
```

`expires_in` is the client's `AuthorizationNonceLifetime` (120 seconds by default). The target application then sends
the nonce to the authorize endpoint in `login_hint_token`, with `acr_values=urn:auth:nonce`.

## Errors

Errors are `400 Bad Request` with a JSON body, including client authentication failures:

* `invalid_client`: the client failed to authenticate;
* `invalid_grant`: the client does not allow the `urn:auth:nonce` grant type;
* `invalid_token`: `token` is missing, invalid or expired, belongs to another client, has no `sub` or the user is no
  longer active.

A client that sent its credentials in the `Authorization` header and fails to authenticate gets `401 Unauthorized`
with `WWW-Authenticate: Basic realm="OidcServer"` instead, as RFC 6749 section 5.2 requires.

[[go back to index]](../index.md#documentation-structure)
