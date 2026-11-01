[[go back to index]](../index.md#documentation-structure)

<a id="refclient"></a>

# Client

The `Client` class (namespace `MinApiLib.Oidc.Server.Core.Models`) models an OpenID Connect or OAuth 2.0 client,
e.g. a native application, a web application, a JavaScript application or a service.

```csharp
new Client
{
    ClientId = "web",
    ClientSecrets = { new Secret("secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.Code,
    RedirectUris = { "https://web.example.com/signin-oidc" },
    PostLogoutRedirectUris = { "https://web.example.com/signout-callback-oidc" },
    AllowedScopes = { "openid", "profile", "orders" },
    AllowOfflineAccess = true,
    RequireDPoP = true
}
```

When the client store returns a client, the server checks its configuration with the registered
`IClientConfigurationValidator` (`DefaultClientConfigurationValidator`, replaced with
`AddClientConfigurationValidator<T>()`): grant types, lifetimes, redirect URIs, CORS origins, URI schemes and
secrets. A client that fails the check is treated as unknown, and an `InvalidClientConfigurationEvent` is raised.

## Basics

`Enabled`
: Specifies if the client is enabled. Defaults to `true`.

`ClientId`
: Unique ID of the client.

`ProtocolType`
: The protocol of the client. Defaults to `oidc`, the only protocol the server implements.

`ClientSecrets`
: List of client secrets (`Secret`, namespace `MinApiLib.Oidc.Server.Core.ClientAuthentication`): credentials to
  access the token endpoint. Only relevant for flows that require a secret. See
  [Client Authentication](../topics/client_authentication.md).

`RequireClientSecret`
: Specifies whether this client needs a secret to request tokens from the token endpoint. Defaults to `true`.

`RequireRequestObject`
: Specifies whether this client must wrap the authorize request parameters in a JWT (a request object). Defaults
  to `false`. See [Request Objects](../topics/request_object.md).

`RequirePushedAuthorization`
: Specifies whether this client must push its authorize requests to the pushed authorization request endpoint
  (RFC 9126) before redirecting the user. Defaults to `false`. `PushedAuthorizationOptions.Required` requires it
  for every client. See [Pushed Authorization Requests](../topics/par.md).

`RequireDPoP`
: Specifies whether token requests of this client must carry a DPoP proof (RFC 9449), so every access token is
  bound to a key the client holds. Defaults to `false`. See [Proof-of-Possession](../topics/pop.md).

`AllowedGrantTypes`
: Specifies the grant types the client is allowed to use. Use the `GrantTypes` class for the common combinations:
  `Code`, `CodeAndClientCredentials`, `Hybrid`, `HybridAndClientCredentials`, `ClientCredentials`, `DeviceFlow`,
  `CodeAndNonce`, and the deprecated `Implicit`, `ImplicitAndClientCredentials`, `ResourceOwnerPassword` and
  `ResourceOwnerPasswordAndClientCredentials`. Grant types cannot contain spaces or repeat, and implicit,
  authorization code and hybrid cannot be combined with each other; setting such a list throws an
  `InvalidOperationException`. See [Grant Types](../topics/grant_types.md).

`RequirePkce`
: Specifies whether clients using an authorization code based grant type must send a proof key. Defaults to `true`.

`AllowPlainTextPkce`
: Specifies whether clients using PKCE can use a plain text code challenge. Not recommended. Defaults to `false`.

`AllowImplicitFlow`
: Enables the deprecated implicit flow (`id_token`, `id_token token` and `token` response types) for a client that
  lists `GrantType.Implicit`. Defaults to `false`; a client that lists the grant without this flag fails
  configuration validation. OAuth 2.1 and RFC 9700 deprecate the implicit flow: use the authorization code flow
  with PKCE instead. See [Grant Types](../topics/grant_types.md#deprecated-flows).

`AllowResourceOwnerPasswordFlow`
: Enables the deprecated resource owner password credentials grant (`grant_type=password`) for a client that lists
  `GrantType.ResourceOwnerPassword`. Defaults to `false`; a client that lists the grant without this flag fails
  configuration validation. See [Resource Owner Password Validation](../topics/resource_owner.md).

`AllowAccessTokensViaBrowser`
: Specifies whether this client can receive access tokens from the authorize endpoint (`token`, `id_token token`,
  `code token` and `code id_token token` response types). Defaults to `false`. Access tokens in the front channel
  are deprecated by OAuth 2.1 and RFC 9700.

`RedirectUris`
: Specifies the allowed URIs to return tokens or authorization codes to. A client with the authorization code,
  hybrid or implicit grant needs at least one.

`RequestUris`
: Pre-registered `request_uri` values the client may send to the authorize endpoint (the OpenID Connect
  `request_uris` client metadata). A `request_uri` that is not listed is rejected. Only used when
  `EndpointsOptions.EnableJwtRequestUri` is enabled. Defaults to empty.

`AllowedScopes`
: By default a client has no access to any resource. List the names of the identity resources and API scopes it
  may request.

`AllowOfflineAccess`
: Specifies whether this client can request refresh tokens (by requesting the `offline_access` scope). Defaults to
  `false`.

`Properties`
: Dictionary to hold any custom client-specific values.

## Authentication/Logout

`PostLogoutRedirectUris`
: Specifies allowed URIs to redirect to after logout. See the
  [OpenID Connect RP-Initiated Logout spec](https://openid.net/specs/openid-connect-rpinitiated-1_0.html).

`FrontChannelLogoutUri`
: Specifies the logout URI at the client for HTTP based front-channel logout. See the
  [OpenID Connect Front-Channel Logout spec](https://openid.net/specs/openid-connect-frontchannel-1_0.html).

`FrontChannelLogoutSessionRequired`
: Specifies if the user's session id should be sent to the `FrontChannelLogoutUri`. Defaults to `true`.

`BackChannelLogoutUri`
: Specifies the logout URI at the client for HTTP based back-channel logout. See the
  [OpenID Connect Back-Channel Logout spec](https://openid.net/specs/openid-connect-backchannel-1_0.html).

`BackChannelLogoutSessionRequired`
: Specifies if the user's session id should be sent in the request to the `BackChannelLogoutUri`. Defaults to `true`.

`EnableLocalLogin`
: Specifies if this client can use local accounts, or external identity providers only. Defaults to `true`. When
  `false`, a session signed in locally does not count for this client: the authorize endpoint sends the user to the
  login page, which should then offer only external providers.

`IdentityProviderRestrictions`
: Specifies which external identity providers can be used with this client. If the list is empty, all are allowed.
  A session from a provider not in the list sends the user to the login page, and an `idp:` value in `acr_values`
  that is not in the list is dropped. Defaults to empty.

`UserSsoLifetime`
: The maximum time, in seconds, since the user last authenticated. When the session is older, the authorize
  endpoint sends the user to the login page again instead of signing them in silently. Defaults to `null` (no
  limit).

## Token

`IdentityTokenLifetime`
: Lifetime of the identity token in seconds. Defaults to 300 seconds (5 minutes).

`AllowedIdentityTokenSigningAlgorithms`
: List of allowed signing algorithms for the identity token. If empty, the server's default signing algorithm is
  used.

`AccessTokenLifetime`
: Lifetime of the access token in seconds. Defaults to 3600 seconds (1 hour).

`AuthorizationCodeLifetime`
: Lifetime of the authorization code in seconds. Defaults to 300 seconds (5 minutes).

`AbsoluteRefreshTokenLifetime`
: Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds (30 days).

`SlidingRefreshTokenLifetime`
: Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds (15 days).

`RefreshTokenUsage`
: `TokenUsage.ReUse`: the refresh token handle stays the same when refreshing tokens.
  <br/>
  `TokenUsage.OneTimeOnly`: the refresh token handle is replaced when refreshing tokens. This is the default.

`RefreshTokenExpiration`
: `TokenExpiration.Absolute`: the refresh token expires at a fixed point in time (set by
  `AbsoluteRefreshTokenLifetime`). This is the default.
  <br/>
  `TokenExpiration.Sliding`: refreshing the token renews its lifetime (by `SlidingRefreshTokenLifetime`). The
  lifetime never exceeds `AbsoluteRefreshTokenLifetime`.

`UpdateAccessTokenClaimsOnRefresh`
: Specifies whether the access token (and its claims) are updated on a refresh token request. Defaults to `false`.

`AccessTokenType`
: Specifies whether the access token is a self-contained JWT (`AccessTokenType.Jwt`, the default) or a reference
  token (`AccessTokenType.Reference`). See [Reference Tokens](../topics/reference_tokens.md).

`IncludeJwtId`
: Specifies whether JWT access tokens have a unique identifier (the `jti` claim). Defaults to `true`.

`AllowedCorsOrigins`
: The origins allowed to call the server's CORS endpoints from JavaScript. Used by the CORS policy services of the
  in-memory and MongoDB stores. See [CORS](../topics/cors.md).

`Claims`
: Claims for the client (`ClientClaim`), included in the access token.

`AlwaysSendClientClaims`
: If set, the client claims are sent for every flow. If not, only for the client credentials flow. Defaults to
  `false`.

`AlwaysIncludeUserClaimsInIdToken`
: When both an identity token and an access token are requested, adds the user claims to the identity token
  instead of requiring the client to use the userinfo endpoint. Defaults to `false`.

`ClientClaimsPrefix`
: The prefix of client claim types, so they do not collide with user claims. Defaults to `client_`.

## Consent Screen

`RequireConsent`
: Specifies whether a consent screen is required. Defaults to `false`.

`AllowRememberConsent`
: Specifies whether the user can choose to store consent decisions. Defaults to `true`.

`ConsentLifetime`
: Lifetime of a user consent in seconds. Defaults to `null` (no expiration).

`ClientName`
: Client display name (used for logging and on the consent screen).

`Description`
: Description of the client.

`ClientUri`
: URI to further information about the client (used on the consent screen).

`LogoUri`
: URI to the client logo (used on the consent screen).

## Device flow

`UserCodeType`
: The type of user code for the client. If not set, `DeviceFlowOptions.DefaultUserCodeType` is used.

`DeviceCodeLifetime`
: Lifetime of the device code in seconds. Defaults to 300 seconds (5 minutes).

## Nonce authorization

`AuthorizationNonceLifetime`
: Lifetime, in seconds, of the one-time tokens the [nonce endpoint](../endpoints/nonce.md) issues to this client.
  Defaults to 120 seconds (2 minutes). See [Nonce Authorization Pattern](nonce_pattern.md).

[[go back to index]](../index.md#documentation-structure)
