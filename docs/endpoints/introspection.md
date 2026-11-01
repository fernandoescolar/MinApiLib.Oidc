[[go back to index]](../index.md#documentation-structure)

# Introspection Endpoint

The introspection endpoint implements [RFC 7662](https://www.rfc-editor.org/rfc/rfc7662). APIs use it to validate
[reference tokens](../topics/reference_tokens.md), or JWTs when they cannot validate them themselves.

| Route | Parameters |
|---|---|
| `POST /connect/introspect` | In an `application/x-www-form-urlencoded` body. Any other content type gets `415` |

With mutual TLS in path mode, the endpoint also answers at `/connect/mtls/introspect`. Any other HTTP method gets
`405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableIntrospectionEndpoint` is `false`.

The caller is an API, not a client: it authenticates with the name of its `ApiResource` and one of the resource's
`ApiSecrets`, in an `Authorization: Basic` header or in the body (see [API resources](../reference/api_resource.md)).

## Parameters

`token`
: the token to introspect (required).

## Example

```http
POST /connect/introspect HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic b3JkZXJzOm9yZGVycy1zZWNyZXQ=

token=8F2A9C1E4B7D6A3F0C5E8B1D4A7C2E9F
```

An active token returns its claims, `active` and the token's scopes that belong to the calling API:

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "iss": "https://login.example.com",
  "nbf": 1800000000,
  "iat": 1800000000,
  "exp": 1800003600,
  "aud": "orders",
  "client_id": "web",
  "sub": "818727",
  "auth_time": 1799999970,
  "idp": "local",
  "jti": "B7F1D2C3E4A5968778695A4B3C2D1E0F",
  "active": true,
  "scope": "orders.read"
}
```

A bound token also returns its `cnf` claim (`jkt` for DPoP, `x5t#S256` for mutual TLS), so the API can check the
binding. An unknown, expired or revoked token, or one without any scope of the calling API, is inactive:

```json
{
  "active": false
}
```

## Errors

* An API that fails to authenticate gets `401`, with no body.
* A request without `token` gets `400` with `{ "error": "missing_token" }`.

[[go back to index]](../index.md#documentation-structure)
