[[go back to index]](../index.md#documentation-structure)

# Pushed Authorization Requests

With pushed authorization requests ([RFC 9126](https://www.rfc-editor.org/rfc/rfc9126)) the client sends the authorize
request parameters to IdentityServer over the back channel first, authenticated like a token request. It then
redirects the browser with only `client_id` and the short-lived `request_uri` it got back. The parameters never travel
through the browser and cannot be tampered with there.

## Flow

1. The client posts the usual authorize parameters to `/connect/par`, authenticating with its secret, a client
   assertion or a certificate. Public clients send only `client_id`.
2. IdentityServer validates the parameters exactly as the authorize endpoint would and answers `201 Created`:

   ```json
   { "request_uri": "urn:ietf:params:oauth:request_uri:…", "expires_in": 600 }
   ```

3. The client redirects the browser to `/connect/authorize?client_id=…&request_uri=…`.

The `request_uri` is single use. It stays valid while the user logs in and consents, and it is consumed when the
authorize endpoint answers the client, successfully or with an error. The return URLs handed to the login and consent
pages carry only `client_id` and `request_uri`.

## Configuration

* `IdentityServerOptions.PushedAuthorization.Lifetime`: seconds a pushed request stays valid (600). It has to cover
  the time the user spends on the login page.
* `IdentityServerOptions.PushedAuthorization.Required`: every client must use PAR. Discovery then announces
  `require_pushed_authorization_requests`.
* `Client.RequirePushedAuthorization`: the same requirement for one client.
* `IdentityServerOptions.Endpoints.EnablePushedAuthorizationEndpoint`: turns the endpoint off (on by default).

Pushed requests are stored as persisted grants of type `pushed_authorization_request`, so every instance has to share
the persisted grant store.

## Security notes

* The `client_id` in the pushed parameters must match the authenticated client.
* `request_uri` is not accepted inside a pushed request.
* Redirect URIs must be registered; the optional RFC 9126 section 2.4 relaxation is not supported.
* A DPoP proof sent to the endpoint binds the resulting authorization code to its key (see [DPoP](pop.md#dpop)).

[[go back to index]](../index.md#documentation-structure)
