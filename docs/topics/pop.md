[[go back to index]](../index.md#documentation-structure)

# Proof-of-Possession Access Tokens

By default, OAuth access tokens are so called *bearer* tokens. This means they are not bound to a client and anybody who possess the token can use it (compare to cash).

*Proof-of-Possession* (short PoP) tokens are bound to the client that requested the token.
If that token leaks, it cannot be used by anyone else (compare to a credit card - well at least in an ideal world).

See [this](https://leastprivilege.com/2020/01/15/oauth-2-0-the-long-road-to-proof-of-possession-access-tokens/) blog post for more history and motivation.

IdentityServer supports PoP tokens by using the [Mutual TLS mechanism](mtls.md#refmutualtls) and DPoP.

## DPoP

With DPoP ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) the client proves possession of a private key it holds
by signing a short JWT, the proof, for every request. No client certificates or TLS configuration are needed.

**Token requests.** When a token request carries a `DPoP` header, IdentityServer validates the proof and binds the
access token to the key: the token carries `cnf: { "jkt": "<thumbprint>" }` and the response says
`"token_type": "DPoP"`. Set `Client.RequireDPoP` to refuse token requests without a proof.

The proof must have `typ: dpop+jwt`, an asymmetric algorithm from `DPoPOptions.SupportedSigningAlgorithms`, a public
`jwk` header and the claims `jti`, `htm`, `htu` and `iat`. Each proof is accepted once. `iat` must fall within
`DPoPOptions.ProofTokenValidityDuration` (one minute) plus `DPoPOptions.ClockSkew` (one minute). Server nonces
(`DPoP-Nonce`) are not supported.

**Authorization codes.** A `dpop_jkt` parameter on the authorize request, or a proof sent to the
[pushed authorization endpoint](par.md), binds the code: only a proof of that key can redeem it.

**Refresh tokens.** Refresh tokens of public clients stay bound to the key they were issued with. Confidential clients
can bind each refreshed access token to the key of the current proof.

**Protected resources.** The userinfo endpoint and the [local API](add_apis.md) handler accept
`Authorization: DPoP <token>` with a proof that carries the token hash (`ath`) and is signed by the bound key. A
DPoP-bound token sent as a bearer token is rejected.

Discovery announces the accepted algorithms in `dpop_signing_alg_values_supported`.

## How DPoP works, step by step

The [samples](../../samples/README.md) run this exchange: a web client signs in with DPoP and calls an API, and its
pages show each request and response.

The example follows a web application, the client `web.dpop`, that signs a user in with the authorization code
flow and then calls an orders API with the user's access token. The values are real: the proofs are signed with
the key below, `jkt` is its thumbprint and `ath` is the hash of the access token. The full encoded tokens are at the
end of the section.

```text
Client (web app)                 IdentityServer                   API (resource server)
     |  1. key pair (private key never leaves the client)
     |  2. GET /connect/authorize?...&dpop_jkt=...  ->|
     |  3. POST /connect/token + DPoP proof         ->|  checks the proof, binds the token
     |<- 4. token_type=DPoP, access token cnf.jkt  ---|
     |  5. GET /orders  Authorization: DPoP <token> + DPoP proof (with ath)  --------------->|
     |                                                    checks token, proof and binding -->|
     |<------------------------------------------------------------------- 6. 200 or 401 --|
```

### 1. The client creates a key pair

The client keeps the private key and only ever sends the public key, as the `jwk` header of each proof. Its
[RFC 7638](https://www.rfc-editor.org/rfc/rfc7638) thumbprint identifies the key:

```json
{
  "kty": "EC",
  "crv": "P-256",
  "x": "p6DvwL4HSd_Ih3ij_YowBaI08yeAsI4dZ3eKK1d9oNI",
  "y": "BGCg2XGMu0eTW6LDOFYehGQigAofRXfHbim4ogjOgUU"
}
```

```text
jkt = BASE64URL(SHA-256('{"crv":"P-256","kty":"EC","x":"…","y":"…"}')) = 8lXgdK2d9cmEBvDiSEnuPezezQAMTqRgsc-rwAr6NEg
```

### 2. Authorize request (optional binding of the code)

The client can bind the authorization code to its key with `dpop_jkt`. Only a proof signed by that key can then
redeem the code, so a stolen code is useless:

```http
GET /connect/authorize?client_id=web.dpop
    &response_type=code
    &scope=openid%20profile%20orders.read%20offline_access
    &redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc
    &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
    &code_challenge_method=S256
    &state=af0ifjsldkj
    &nonce=n-0S6_WzA2Mj
    &dpop_jkt=8lXgdK2d9cmEBvDiSEnuPezezQAMTqRgsc-rwAr6NEg HTTP/1.1
Host: login.example.com
```

With [pushed authorization requests](par.md) the client sends a `DPoP` proof to `/connect/par` instead, and the
server binds the code to the key of that proof.

### 3. Token request with a proof

The client creates a new proof for the token request: a JWT with `typ: dpop+jwt`, the public key in `jwk`, the
HTTP method and URL of the request, a unique `jti` and the current time. It sends it in the `DPoP` header:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic d2ViLmRwb3A6c2VjcmV0
DPoP: eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2…

grant_type=authorization_code
&code=SplxlOBeZQQYbYS6WxSbIA
&redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc
&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
```

The proof, decoded:

```json
{
  "typ": "dpop+jwt",
  "alg": "ES256",
  "jwk": {
    "kty": "EC",
    "crv": "P-256",
    "x": "p6DvwL4HSd_Ih3ij_YowBaI08yeAsI4dZ3eKK1d9oNI",
    "y": "BGCg2XGMu0eTW6LDOFYehGQigAofRXfHbim4ogjOgUU"
  }
}
.
{
  "jti": "e1j3V_bKic8-LAEB",
  "htm": "POST",
  "htu": "https://login.example.com/connect/token",
  "iat": 1800000000
}
.
<ES256 signature with the client's private key>
```

IdentityServer checks the proof: the type, an algorithm from `DPoPOptions.SupportedSigningAlgorithms`, the signature
with the key in `jwk`, `htm` and `htu` against this request, `iat` within one minute (plus one minute of clock skew)
and that `jti` was not used before. When the code was bound with `dpop_jkt`, the proof key must be that one.

### 4. Token response: a DPoP-bound access token

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store

{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…",
  "token_type": "DPoP",
  "expires_in": 3600,
  "scope": "openid profile orders.read offline_access",
  "id_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJBMUQzQiIsInR5cCI6IkpXVCJ9…",
  "refresh_token": "8xLOxBtZp8"
}
```

`token_type` is `DPoP` and the access token carries the thumbprint of the proof key in `cnf.jkt`:

```json
{
  "alg": "RS256",
  "kid": "8C5C4F2A1D3B",
  "typ": "at+jwt"
}
.
{
  "iss": "https://login.example.com",
  "nbf": 1800000000,
  "iat": 1800000000,
  "exp": 1800003600,
  "aud": "orders",
  "scope": [
    "openid",
    "profile",
    "orders.read"
  ],
  "amr": [
    "pwd"
  ],
  "client_id": "web.dpop",
  "sub": "818727",
  "auth_time": 1799999970,
  "idp": "local",
  "sid": "4F3C0E6D2B1A",
  "jti": "B7F1D2C3E4A5968778695A4B3C2D1E0F",
  "cnf": {
    "jkt": "8lXgdK2d9cmEBvDiSEnuPezezQAMTqRgsc-rwAr6NEg"
  }
}
```

A request without a `DPoP` header gets a `"token_type": "Bearer"` token, unless the client has
`RequireDPoP = true`, in which case the request fails with `invalid_request` and the description
`DPoP proof required`.

### 5. Calling the API

For every call the client creates a new proof for that request, with the hash of the access token in `ath`, and sends
the token with the `DPoP` scheme instead of `Bearer`:

```http
GET /orders HTTP/1.1
Host: api.example.com
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
DPoP: eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2…
```

The proof, decoded:

```json
{
  "typ": "dpop+jwt",
  "alg": "ES256",
  "jwk": {
    "kty": "EC",
    "crv": "P-256",
    "x": "p6DvwL4HSd_Ih3ij_YowBaI08yeAsI4dZ3eKK1d9oNI",
    "y": "BGCg2XGMu0eTW6LDOFYehGQigAofRXfHbim4ogjOgUU"
  }
}
.
{
  "jti": "Q2N-g0dVtt8_bF5w",
  "htm": "GET",
  "htu": "https://api.example.com/orders",
  "iat": 1800000060,
  "ath": "cz8bqK2SSlQIPgtFQ0XkjcrYuLyIfwlXUziLOXfMDpk"
}
```

```text
ath = BASE64URL(SHA-256(access_token)) = cz8bqK2SSlQIPgtFQ0XkjcrYuLyIfwlXUziLOXfMDpk
```

### 6. What the API checks

1. The access token, as for any bearer token: signature, issuer, audience, lifetime and scope.
2. The proof: `typ`, algorithm and signature with its `jwk`, `htm` = `GET`, `htu` = `https://api.example.com/orders`,
   a recent `iat` and a `jti` not seen before.
3. `ath` is the hash of the access token received in the `Authorization` header.
4. The thumbprint of the proof's `jwk` equals `cnf.jkt` in the access token.
5. A token with `cnf.jkt` sent with the `Bearer` scheme is rejected: otherwise a stolen token would still work
   without the key.

When everything matches:

```http
HTTP/1.1 200 OK
Content-Type: application/json

[{"id":1,"total":42.0}]
```

Someone who stole the access token, but not the private key, cannot produce a valid proof:

```http
GET /orders HTTP/1.1
Host: api.example.com
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
DPoP: <proof signed with another key>
```

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: DPoP error="invalid_token", error_description="DPoP proof key does not match the key the access token is bound to", algs="ES256 PS256"
```

Sending the stolen token as a bearer token fails as well, and so does replaying a captured proof (the same `jti`) or
using it against another URL (`htu`):

```http
GET /orders HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
```

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: DPoP error="invalid_token", error_description="DPoP-bound access token sent without the DPoP scheme", algs="ES256 PS256"
```

The IdentityServer userinfo endpoint and the [local API](add_apis.md) handler perform these checks themselves. Their
error responses use the `Bearer` challenge: `WWW-Authenticate: Bearer realm="IdentityServer", error="invalid_token",
error_description="..."`.

### Refreshing the token

The client refreshes with a new proof of the same key. Refresh tokens issued to public clients are bound to the key,
so only the original key can use them:

```http
POST /connect/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded
Authorization: Basic d2ViLmRwb3A6c2VjcmV0
DPoP: eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2…

grant_type=refresh_token&refresh_token=8xLOxBtZp8
```

```json
{
  "jti": "x9pV1gM2dQ4-rT7k",
  "htm": "POST",
  "htu": "https://login.example.com/connect/token",
  "iat": 1800003500
}
```

### Compared with a bearer token

| | Bearer | DPoP |
|---|---|---|
| Token request | No proof | `DPoP` header with a proof of the client key |
| `token_type` | `Bearer` | `DPoP` |
| Access token | No `cnf` | `cnf.jkt` with the key thumbprint |
| API request | `Authorization: Bearer <token>` | `Authorization: DPoP <token>` and a new `DPoP` proof with `ath` |
| Stolen access token | Works until it expires | Useless without the private key |

<details>
<summary>Full encoded values of the example</summary>

Proof of the token request:

```text
eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2IiwiandrIjp7Imt0eSI6IkVDIiwiY3J2IjoiUC0yNTYiLCJ4IjoicDZEdndMNEhTZF9JaDNpal9Zb3dCYUkwOHllQXNJNGRaM2VLSzFkOW9OSSIsInkiOiJCR0NnMlhHTXUwZVRXNkxET0ZZZWhHUWlnQW9mUlhmSGJpbTRvZ2pPZ1VVIn19.eyJqdGkiOiJlMWozVl9iS2ljOC1MQUVCIiwiaHRtIjoiUE9TVCIsImh0dSI6Imh0dHBzOi8vbG9naW4uZXhhbXBsZS5jb20vY29ubmVjdC90b2tlbiIsImlhdCI6MTgwMDAwMDAwMH0.DxtQ-u6yD3pJXYVWTQ_NZfpoBWANMX1ViiW0YEA_LaKjwfsXtVpb4XK1LMuk2r14ongS5Geh1McNnx1OoHsaoA
```

Access token (signed with an example RSA key):

```text
eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJBMUQzQiIsInR5cCI6ImF0K2p3dCJ9.eyJpc3MiOiJodHRwczovL2xvZ2luLmV4YW1wbGUuY29tIiwibmJmIjoxODAwMDAwMDAwLCJpYXQiOjE4MDAwMDAwMDAsImV4cCI6MTgwMDAwMzYwMCwiYXVkIjoib3JkZXJzIiwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsIm9yZGVycy5yZWFkIl0sImFtciI6WyJwd2QiXSwiY2xpZW50X2lkIjoid2ViLmRwb3AiLCJzdWIiOiI4MTg3MjciLCJhdXRoX3RpbWUiOjE3OTk5OTk5NzAsImlkcCI6ImxvY2FsIiwic2lkIjoiNEYzQzBFNkQyQjFBIiwianRpIjoiQjdGMUQyQzNFNEE1OTY4Nzc4Njk1QTRCM0MyRDFFMEYiLCJjbmYiOnsiamt0IjoiOGxYZ2RLMmQ5Y21FQnZEaVNFbnVQZXplelFBTVRxUmdzYy1yd0FyNk5FZyJ9fQ.CrGWUthieqD3PAYBT440Wefu-sxEJq3svSHLsX58vfT7xOAMBDSw1AEmEhAY9Sc6dV4zMRE0BFe5xHzLIcPWtLjqk-XzGfyfPSQ_SsACUlLPN3jxXHKvKKoA7on882XWo-MTZEN6YCirdO_9z28BBTWV4t4-AOYFMpCK3X_1KZ_Yl-PnA_tY5ZyMIn4KUlumNL1ymJ-h_S6WP_afPGPt6haPmC3lwZicw13gOF1gjf_NMbGly88CyzbxpQ-NsFLLL_bjqn2F8Cxd0Mcv--JAByzafM2Be5LinPieaE-IE5ERM1vpmTrx6tmhpOkRTOXG4Uo9AXteAH5PYUy8_vzYDA
```

Proof of the API request:

```text
eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2IiwiandrIjp7Imt0eSI6IkVDIiwiY3J2IjoiUC0yNTYiLCJ4IjoicDZEdndMNEhTZF9JaDNpal9Zb3dCYUkwOHllQXNJNGRaM2VLSzFkOW9OSSIsInkiOiJCR0NnMlhHTXUwZVRXNkxET0ZZZWhHUWlnQW9mUlhmSGJpbTRvZ2pPZ1VVIn19.eyJqdGkiOiJRMk4tZzBkVnR0OF9iRjV3IiwiaHRtIjoiR0VUIiwiaHR1IjoiaHR0cHM6Ly9hcGkuZXhhbXBsZS5jb20vb3JkZXJzIiwiaWF0IjoxODAwMDAwMDYwLCJhdGgiOiJjejhicUsyU1NsUUlQZ3RGUTBYa2pjcll1THlJZndsWFV6aUxPWGZNRHBrIn0.aHVc5oAd1UlJLWIef9K4-SNGi9vg685uYUlqIP5g6xAs_dmBmijjFrwIsA2OmI_I3ceiG1KlmhUYvIUlOHsbyQ
```

Proof of the refresh request:

```text
eyJ0eXAiOiJkcG9wK2p3dCIsImFsZyI6IkVTMjU2IiwiandrIjp7Imt0eSI6IkVDIiwiY3J2IjoiUC0yNTYiLCJ4IjoicDZEdndMNEhTZF9JaDNpal9Zb3dCYUkwOHllQXNJNGRaM2VLSzFkOW9OSSIsInkiOiJCR0NnMlhHTXUwZVRXNkxET0ZZZWhHUWlnQW9mUlhmSGJpbTRvZ2pPZ1VVIn19.eyJqdGkiOiJ4OXBWMWdNMmRRNC1yVDdrIiwiaHRtIjoiUE9TVCIsImh0dSI6Imh0dHBzOi8vbG9naW4uZXhhbXBsZS5jb20vY29ubmVjdC90b2tlbiIsImlhdCI6MTgwMDAwMzUwMH0.4RiBEijKE72Z5JRkp5WDd-Ufz6fxxdd_8wwwDAqSHwvsAeoaLtt-GIhNhGeHI8c6GpOmDDiRlzdIC3V-UlCTJQ
```

</details>

## How mutual TLS works, step by step

With [mutual TLS](mtls.md) ([RFC 8705](https://www.rfc-editor.org/rfc/rfc8705)) the proof is the TLS client
certificate. The client authenticates with its certificate at the mTLS token endpoint, and the access token is bound
to the certificate's SHA-256 thumbprint.

The token request goes to the `mtls` alias of the endpoint (or to the mTLS domain), over a TLS connection where
the client presents its certificate:

```http
POST /connect/mtls/token HTTP/1.1
Host: login.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=orders.worker&scope=orders.read
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJBMUQzQiIsInR5cCI6ImF0K2p3dCJ9…",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "orders.read"
}
```

The token type stays `Bearer`, but the token carries the certificate thumbprint:

```json
{
  "iss": "https://login.example.com",
  "aud": "orders",
  "client_id": "orders.worker",
  "scope": ["orders.read"],
  "cnf": { "x5t#S256": "bwcK0esc3ACC3DB2Y5_lESsXE8o9ltc05O89jdN-dg2" }
}
```

The client calls the API over mTLS with the same certificate:

```http
GET /orders HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJBMUQzQiIsInR5cCI6ImF0K2p3dCJ9…
```

The API validates the token, computes the SHA-256 thumbprint of the certificate of the TLS connection and compares it
with `cnf.x5t#S256`. If the token is replayed over a connection without that certificate, the API answers:

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", error_description="The access token is bound to another certificate"
```

[[go back to index]](../index.md#documentation-structure)
