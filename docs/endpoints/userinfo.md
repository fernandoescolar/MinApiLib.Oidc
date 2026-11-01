[[go back to index]](../index.md#documentation-structure)

# UserInfo Endpoint

The UserInfo endpoint returns claims about the user (see the
[specification](https://openid.net/specs/openid-connect-core-1_0.html#UserInfo)).

| Route | Access token |
|---|---|
| `GET /connect/userinfo` | In the `Authorization` header |
| `POST /connect/userinfo` | In the `Authorization` header, or as `access_token` in an `application/x-www-form-urlencoded` body |

Both methods are processed the same way. With mutual TLS in path mode, the endpoint also answers at
`/connect/mtls/userinfo`. Any other HTTP method gets `405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableUserInfoEndpoint` is `false`.

The caller sends an access token issued for the user with the `openid` scope. The endpoint returns the `sub` claim
and the claims of the identity scopes the token was granted, as the `IProfileService` provides them (see
[profile service](../reference/profileservice.md)).

## Example

```http
GET /connect/userinfo HTTP/1.1
Host: login.example.com
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "sub": "818727",
  "name": "Alice Smith",
  "given_name": "Alice",
  "family_name": "Smith"
}
```

## DPoP-bound tokens

A DPoP-bound access token (with `cnf.jkt`) must be sent with the `DPoP` scheme and a proof of the bound key that
carries the token hash (`ath`), as for any protected resource ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)
section 7). The same token sent as a bearer token is rejected.

```http
GET /connect/userinfo HTTP/1.1
Host: login.example.com
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
DPoP: eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2…
```

See [proof of possession](../topics/pop.md#dpop).

## Errors

A missing, invalid or expired token, a token without `sub`, an inactive user or a failed DPoP check get `401` with
`invalid_token` in the `WWW-Authenticate` header. A token without the `openid` scope gets `403` with
`insufficient_scope`.

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer realm="OidcServer",error="invalid_token",error_description="The access token expired"
```

[[go back to index]](../index.md#documentation-structure)
