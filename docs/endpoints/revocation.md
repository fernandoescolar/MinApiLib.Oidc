[[go back to index]](../index.md#documentation-structure)

# Revocation Endpoint

The revocation endpoint implements [RFC 7009](https://www.rfc-editor.org/rfc/rfc7009). Clients use it to revoke
refresh tokens and [reference access tokens](../topics/reference_tokens.md). JWT access tokens cannot be revoked:
they stay valid until they expire.

| Route | Parameters |
|---|---|
| `POST /connect/revocation` | In an `application/x-www-form-urlencoded` body. Any other content type gets `415` |

With mutual TLS in path mode, the endpoint also answers at `/connect/mtls/revocation`. Any other HTTP method gets
`405` with an `Allow` header. The endpoint answers `404` when
`OidcServerOptions.Endpoints.EnableTokenRevocationEndpoint` is `false`.

The client authenticates as it would at the [token endpoint](token.md); public clients send only `client_id`. A
client can only revoke its own tokens.

## Parameters

`token`
: the token to revoke (required).

`token_type_hint`
: `access_token` or `refresh_token` (optional). With a hint, the server only looks for that type of token. Without
  one, it looks for a reference access token first and then for a refresh token.

## Example

```http
POST /connect/revocation HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic d2ViOndlYi1zZWNyZXQ=

token=8xLOxBtZp8&token_type_hint=refresh_token
```

```http
HTTP/1.1 200 OK
```

The answer is `200` also when the token is unknown or belongs to another client, as RFC 7009 requires. Revoking a
refresh token also removes the reference access tokens issued to the same user and client.

## Errors

Errors are `400 Bad Request` with a JSON body:

* `invalid_client`: the client failed to authenticate;
* `invalid_request`: `token` is missing;
* `unsupported_token_type`: `token_type_hint` is neither `access_token` nor `refresh_token`.

A client that sent its credentials in the `Authorization` header and fails to authenticate gets `401 Unauthorized`
with `WWW-Authenticate: Basic realm="OidcServer"` instead, as RFC 6749 section 5.2 requires.

[[go back to index]](../index.md#documentation-structure)
