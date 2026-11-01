[[go back to index]](../index.md#documentation-structure)

# Discovery Endpoint

The discovery endpoint returns the server's metadata: the issuer name, the endpoint URLs, the signing keys, the
supported scopes, grant types and so on. See the
[specification](https://openid.net/specs/openid-connect-discovery-1_0.html) for details.

| Route | Returns |
|---|---|
| `GET /.well-known/openid-configuration` | The discovery document |
| `GET /.well-known/openid-configuration/jwks` | The public signing keys (JWKS), at the document's `jwks_uri` |

Any other HTTP method gets `405` with an `Allow` header. Both routes answer `404` when
`OidcServerOptions.Endpoints.EnableDiscoveryEndpoint` is `false`; the key set also when `Discovery.ShowKeySet` is
`false`. With `Discovery.ResponseCacheInterval` set, both responses carry `Cache-Control: max-age=<seconds>`. What the
document contains, and how to add entries to it, is described in [discovery](../topics/discovery.md).

## Example

```http
GET /.well-known/openid-configuration HTTP/1.1
Host: login.example.com
```

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=UTF-8

{
  "issuer": "https://login.example.com",
  "jwks_uri": "https://login.example.com/.well-known/openid-configuration/jwks",
  "authorization_endpoint": "https://login.example.com/connect/authorize",
  "token_endpoint": "https://login.example.com/connect/token",
  "userinfo_endpoint": "https://login.example.com/connect/userinfo",
  "end_session_endpoint": "https://login.example.com/connect/endsession",
  "check_session_iframe": "https://login.example.com/connect/checksession",
  "revocation_endpoint": "https://login.example.com/connect/revocation",
  "introspection_endpoint": "https://login.example.com/connect/introspect",
  "device_authorization_endpoint": "https://login.example.com/connect/deviceauthorization",
  "pushed_authorization_request_endpoint": "https://login.example.com/connect/par",
  "nonce_endpoint": "https://login.example.com/connect/nonce",
  "frontchannel_logout_supported": true,
  "frontchannel_logout_session_supported": true,
  "backchannel_logout_supported": true,
  "backchannel_logout_session_supported": true,
  "scopes_supported": ["openid", "profile", "orders.read", "offline_access"],
  "claims_supported": ["sub", "name", "family_name", "given_name", "…"],
  "grant_types_supported": [
    "authorization_code",
    "client_credentials",
    "refresh_token",
    "urn:ietf:params:oauth:grant-type:device_code"
  ],
  "response_types_supported": ["code", "code id_token"],
  "response_modes_supported": ["form_post", "query", "fragment"],
  "token_endpoint_auth_methods_supported": ["client_secret_basic", "client_secret_post"],
  "id_token_signing_alg_values_supported": ["RS256"],
  "subject_types_supported": ["public"],
  "code_challenge_methods_supported": ["S256"],
  "request_parameter_supported": true,
  "prompt_values_supported": ["none", "login", "consent", "select_account"],
  "request_object_signing_alg_values_supported": ["RS256", "RS384", "RS512", "PS256", "PS384", "PS512", "ES256", "ES384", "ES512"],
  "authorization_response_iss_parameter_supported": true,
  "dpop_signing_alg_values_supported": ["RS256", "RS384", "RS512", "PS256", "PS384", "PS512", "ES256", "ES384", "ES512"]
}
```

The values follow the configuration: the registered extension grant types are added to `grant_types_supported`,
`mtls_endpoint_aliases` appears when mutual TLS is enabled and `require_pushed_authorization_requests` when
`PushedAuthorization.Required` is set. The deprecated response types (implicit, and those that return an access token
from the authorize endpoint) and the `password` grant are never published, although clients that opt in can use them.

## Clients

Microsoft's OpenID Connect and JWT bearer handlers, and
[MinApiLib.Oidc.Http](../../src/MinApiLib.Oidc.Http/README.md), read the discovery document from the authority you
configure: you rarely need to call the endpoint yourself.

[[go back to index]](../index.md#documentation-structure)
