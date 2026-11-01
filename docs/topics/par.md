[[go back to index]](../index.md#documentation-structure)

# Pushed Authorization Requests

With pushed authorization requests ([RFC 9126](https://www.rfc-editor.org/rfc/rfc9126)) the client sends the authorize
request parameters to the server over the back channel first, authenticated like a token request. It then redirects
the browser with only `client_id` and the short-lived `request_uri` it got back. The parameters never travel through
the browser and cannot be tampered with there.

## Flow

1. The client posts the usual authorize parameters to `POST /connect/par`, authenticating with its secret, a client
   assertion or a certificate. Public clients send only `client_id`.

   ```http
   POST /connect/par HTTP/1.1
   Host: login.example.com
   Content-Type: application/x-www-form-urlencoded
   Authorization: Basic d2ViOndlYi1zZWNyZXQ=

   response_type=code
   &scope=openid%20profile%20orders.read
   &redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc
   &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
   &code_challenge_method=S256
   &state=af0ifjsldkj
   ```

2. The server validates the parameters exactly as the authorize endpoint would and answers `201 Created`:

   ```http
   HTTP/1.1 201 Created
   Content-Type: application/json

   { "request_uri": "urn:ietf:params:oauth:request_uri:…", "expires_in": 600 }
   ```

3. The client redirects the browser to `/connect/authorize?client_id=…&request_uri=…`.

The `request_uri` is single use. It stays valid while the user logs in and consents, and it is consumed when the
authorize endpoint answers the client, successfully or with an error. The return URLs handed to the login and consent
pages carry only `client_id` and `request_uri`.

A failed client authentication gets `401` with `invalid_client`; invalid parameters get `400` with the error the
authorize endpoint would return. A body that is not `application/x-www-form-urlencoded` gets `415`.

Microsoft's OpenID Connect handler (.NET 9 and later) pushes its authorize requests by itself when the discovery
document announces the endpoint (`OpenIdConnectOptions.PushedAuthorizationBehavior`, `UseIfAvailable` by default).

## Configuration

* `OidcServerOptions.PushedAuthorization.Lifetime`: seconds a pushed request stays valid (600). It has to cover the
  time the user spends on the login page.
* `OidcServerOptions.PushedAuthorization.Required`: every client must use PAR. Discovery then announces
  `require_pushed_authorization_requests`.
* `Client.RequirePushedAuthorization`: the same requirement for one client.
* `OidcServerOptions.Endpoints.EnablePushedAuthorizationEndpoint`: turns the endpoint off (on by default). While it is
  off, the requirements above are not enforced.

Discovery announces the endpoint as `pushed_authorization_request_endpoint`. With mutual TLS in path mode it also
answers at `/connect/mtls/par`.

Pushed requests are stored as persisted grants of type `pushed_authorization_request`, so every instance has to share
the persisted grant store (see [persisted grants](persisted_grants.md)).

## Security notes

* The `client_id` in the pushed parameters must match the authenticated client.
* `request_uri` is not accepted inside a pushed request.
* Redirect URIs must be registered; the optional RFC 9126 section 2.4 relaxation is not supported.
* A DPoP proof sent to the endpoint binds the resulting authorization code to its key (see [DPoP](pop.md#dpop)). A
  `dpop_jkt` parameter sent with it must match the key of the proof.

[[go back to index]](../index.md#documentation-structure)
