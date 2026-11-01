[[go back to index]](../index.md#documentation-structure)

# Supported Specifications

`MinApiLib.Oidc.Server` implements the following specifications. The client packages (`MinApiLib.Oidc.Http`,
`MinApiLib.Oidc.Authentication`) and `MinApiLib.Oidc.JwtBearer` implement the client and resource server sides of
OAuth 2.0 bearer tokens and DPoP.

## OpenID Connect

* OpenID Connect Core 1.0 ([spec](https://openid.net/specs/openid-connect-core-1_0.html))
* OpenID Connect Discovery 1.0 ([spec](https://openid.net/specs/openid-connect-discovery-1_0.html))
* OpenID Connect RP-Initiated Logout 1.0 ([spec](https://openid.net/specs/openid-connect-rpinitiated-1_0.html)),
  including `client_id` without an `id_token_hint`
* OpenID Connect Session Management 1.0 ([spec](https://openid.net/specs/openid-connect-session-1_0.html))
* OpenID Connect Front-Channel Logout 1.0 ([spec](https://openid.net/specs/openid-connect-frontchannel-1_0.html))
* OpenID Connect Back-Channel Logout 1.0 ([spec](https://openid.net/specs/openid-connect-backchannel-1_0.html)),
  with logout tokens of type `logout+jwt`
* OAuth 2.0 Multiple Response Types ([spec](https://openid.net/specs/oauth-v2-multiple-response-types-1_0.html))
* OAuth 2.0 Form Post Response Mode ([spec](https://openid.net/specs/oauth-v2-form-post-response-mode-1_0.html))

## OAuth 2.0

* The OAuth 2.0 Authorization Framework ([RFC 6749](https://www.rfc-editor.org/rfc/rfc6749))
* OAuth 2.0 Bearer Token Usage ([RFC 6750](https://www.rfc-editor.org/rfc/rfc6750))
* OAuth 2.0 Token Revocation ([RFC 7009](https://www.rfc-editor.org/rfc/rfc7009))
* OAuth 2.0 Token Introspection ([RFC 7662](https://www.rfc-editor.org/rfc/rfc7662))
* Proof Key for Code Exchange ([RFC 7636](https://www.rfc-editor.org/rfc/rfc7636)). Discovery announces `S256`
  only; `plain` needs `Client.AllowPlainTextPkce`.
* JSON Web Token (JWT) Profile for OAuth 2.0 Client Authentication ([RFC 7523](https://www.rfc-editor.org/rfc/rfc7523)),
  `private_key_jwt` with `AddJwtBearerClientAuthentication()`
* OAuth 2.0 for Native Apps ([RFC 8252](https://www.rfc-editor.org/rfc/rfc8252)): loopback redirect URIs with
  `AddAppAuthRedirectUriValidator()`
* OAuth 2.0 Device Authorization Grant ([RFC 8628](https://www.rfc-editor.org/rfc/rfc8628))
* OAuth 2.0 Mutual-TLS Client Authentication and Certificate-Bound Access Tokens ([RFC 8705](https://www.rfc-editor.org/rfc/rfc8705))
* JWT-Secured Authorization Request (JAR) ([RFC 9101](https://www.rfc-editor.org/rfc/rfc9101)). A `request_uri`
  must be registered in `Client.RequestUris`.

## Added since IdentityServer4

IdentityServer4 4.1.2 did not implement these. See [CHANGELOG.md](../../CHANGELOG.md) for the details.

* OAuth 2.0 Pushed Authorization Requests ([RFC 9126](https://www.rfc-editor.org/rfc/rfc9126)): `/connect/par`, on
  by default; `Client.RequirePushedAuthorization`. See [Pushed Authorization Requests](../topics/par.md).
* OAuth 2.0 Demonstrating Proof of Possession, DPoP ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)): proofs at
  the token endpoint, `cnf.jkt` binding, `dpop_jkt` and PAR proofs that bind the authorization code, and
  `Client.RequireDPoP`. The userinfo endpoint and the local API handler enforce the binding. The server does not issue
  DPoP nonces; `MinApiLib.Oidc.JwtBearer` can, for APIs (`RequireNonce`). See
  [Proof-of-Possession Access Tokens](../topics/pop.md).
* Resource Indicators for OAuth 2.0 ([RFC 8707](https://www.rfc-editor.org/rfc/rfc8707)): the `resource` parameter
  at the authorize and token endpoints narrows the access token's audience; `ApiResource.RequireResourceIndicator`.
* OAuth 2.0 Authorization Server Issuer Identification ([RFC 9207](https://www.rfc-editor.org/rfc/rfc9207)): `iss`
  in authorization responses, on by default (`EmitIssuerIdentificationResponseParameter`), announced in discovery as
  `authorization_response_iss_parameter_supported`.
* JSON Web Token (JWT) Profile for OAuth 2.0 Access Tokens ([RFC 9068](https://www.rfc-editor.org/rfc/rfc9068)):
  access tokens have type `at+jwt` (`AccessTokenJwtType`, as in IdentityServer4) and now always carry an audience.
* `client_id` at the end session endpoint (RP-Initiated Logout 1.0): validates `post_logout_redirect_uri` without
  an `id_token_hint`. The sign-out prompt is still shown.
* OpenID Connect Core Error Code `unmet_authentication_requirements`
  ([spec](https://openid.net/specs/openid-connect-unmet-authentication-requirements-1_0.html)): returned when
  `Authentication.EnforceAcrValues` is on and the session does not meet the requested `acr_values`.
* Custom `prompt` values, announced in `prompt_values_supported`: for example `create` from Initiating User
  Registration via OpenID Connect ([spec](https://openid.net/specs/openid-connect-prompt-create-1_0.html)), with
  `AddPromptRedirect(prompt, url)` or `AddPromptHandler<T>(prompt)`. See [Custom prompt values](../topics/prompt.md).

The server follows the OAuth 2.0 Security Best Current Practice ([RFC 9700](https://www.rfc-editor.org/rfc/rfc9700)):
the implicit flow, access tokens from the authorize endpoint and the resource owner password grant are off by
default, behind `Client.AllowImplicitFlow`, `Client.AllowAccessTokensViaBrowser` and
`Client.AllowResourceOwnerPasswordFlow`. Redirect URIs are compared exactly, and a reused refresh token revokes the
subject's refresh tokens for that client.

## Used by the samples

The [sample server](../../samples/README.md) signs users in with a password or a passkey and says how in the tokens.
The server passes these values through; it does not define them:

* Authentication Method Reference Values ([RFC 8176](https://www.rfc-editor.org/rfc/rfc8176)): `amr` is `pwd` for a
  password, and `hwk` (a key on the device) or `swk` (a synced passkey) with `mfa` (the authenticator verified the
  user) or `user` (user presence only) for a passkey.
* OpenID Connect Extended Authentication Profile (EAP) ACR Values 1.0
  ([spec](https://openid.net/specs/openid-connect-eap-acr-values-1_0.html)): `acr` is `phr` (phishing-resistant)
  for a passkey, and a client asks for it with `acr_values=phr`.

[[go back to index]](../index.md#documentation-structure)
