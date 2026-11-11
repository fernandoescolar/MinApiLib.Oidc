[[go back to index]](../index.md#documentation-structure)

<a id="refoptions"></a>

# Server Options

`OidcServerOptions` (namespace `MinApiLib.Oidc.Server.Core.Configuration`) holds the server's settings. Set them
when you register the server:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.IssuerUri = "https://login.example.com";
    options.KeyManagement.Enabled = true;
    options.Events.RaiseErrorEvents = true;
});
```

`AddOidcServer(IConfiguration)` binds them from a configuration section instead, for example
`builder.Services.AddOidcServer(builder.Configuration.GetSection("OidcServer"))`.

## Startup validation

The options are validated when the host starts (an `IValidateOptions<OidcServerOptions>` registered with
`ValidateOnStart`). Inconsistent values stop the host with an `OptionsValidationException` that lists every problem,
instead of failing on the first request that reads them. The checks:

* `IssuerUri`, when set, is an absolute `http` or `https` URL without query or fragment (RFC 8414 section 2).
* `Authentication.CookieLifetime` is positive.
* Every `InputLengthRestrictions` value is positive.
* `DeviceFlow.Interval` is positive, and `DeviceFlow.UserCodeMaxFailedAttempts` is not negative.
* `PushedAuthorization.Lifetime` is positive.
* `DPoP.ProofTokenValidityDuration` is positive, `DPoP.ClockSkew` is not negative, and
  `DPoP.SupportedSigningAlgorithms` is a non-empty list of RS\*, PS\* or ES\* algorithms.
* When `KeyManagement.Enabled` is set: `SigningAlgorithms` is a non-empty list of RS\*, PS\* or ES\* algorithms,
  `PropagationTime` is positive, `RotationInterval` is longer than `PropagationTime`, `RetentionDuration` is not
  negative, and `KeyCacheDuration` is positive and shorter than `PropagationTime`.

## Top-level settings

* `IssuerUri`
  : The issuer name that appears in the discovery document and in the issued tokens, e.g.
    `https://login.example.com`. If not set, the issuer is inferred from the request's host. Defaults to `null`.
* `LowerCaseIssuerUri`
  : Set to `false` to keep the casing of the issuer. Defaults to `true`.
* `AccessTokenJwtType`
  : The JWT `typ` header of access tokens. Defaults to `at+jwt`.
* `LogoutTokenJwtType`
  : The JWT `typ` header of back-channel logout tokens (OpenID Connect Back-Channel Logout 1.0, section 2.4).
    Defaults to `logout+jwt`.
* `EmitIssuerIdentificationResponseParameter`
  : Adds the `iss` parameter to authorization responses (RFC 9207). It protects clients that talk to several
    authorization servers against mix-up attacks. Defaults to `true`.
* `EmitStaticAudienceClaim`
  : Adds an `aud` claim with the format `issuer/resources` to every access token, for older access token
    validation code. Defaults to `false`. Access tokens with no API resource get this audience anyway, since
    RFC 9068 requires an `aud` claim.
* `EmitScopesAsSpaceDelimitedStringInJwt`
  : Emits the `scope` claim of JWTs as a space-delimited string instead of an array. Defaults to `false`.
* `StrictJarValidation`
  : Checks the JWT `typ` and content type of request objects (JWT-secured authorization requests) as the IETF
    specification requires. It can break older OpenID Connect request objects. Defaults to `false`.

## Endpoints

`EndpointsOptions`. Every endpoint is enabled by default. A disabled endpoint answers 404, and discovery no longer
lists it.

* `EnableAuthorizeEndpoint`, `EnableTokenEndpoint`, `EnableUserInfoEndpoint`, `EnableDiscoveryEndpoint`,
  `EnableEndSessionEndpoint`, `EnableCheckSessionEndpoint`, `EnableTokenRevocationEndpoint`,
  `EnableIntrospectionEndpoint`, `EnableDeviceAuthorizationEndpoint`, `EnableNonceEndpoint`,
  `EnablePushedAuthorizationEndpoint`
  : Enable or disable each endpoint. Default to `true`.
* `EnableJwtRequestUri`
  : Enables `request_uri` processing on the authorize endpoint (request objects fetched by URL). Only the URLs a
    client lists in `Client.RequestUris` are accepted. Defaults to `false`.

## Discovery

`DiscoveryOptions` (namespace `MinApiLib.Oidc.Server.Features.Discovery`). The `Show*` settings default to `true`.

* `ShowEndpoints`, `ShowKeySet`, `ShowIdentityScopes`, `ShowApiScopes`, `ShowClaims`, `ShowResponseTypes`,
  `ShowResponseModes`, `ShowGrantTypes`, `ShowExtensionGrantTypes`, `ShowTokenEndpointAuthenticationMethods`
  : Include or leave out each section of the discovery document.
* `ExpandRelativePathsInCustomEntries`
  : Turns relative paths that start with `~/` in `CustomEntries` into absolute URLs. Defaults to `true`.
* `ResponseCacheInterval`
  : The `max-age` of the `Cache-Control` header of the discovery response, in seconds. `0` sets no-cache headers.
    Defaults to `null`, which sets no header.
* `CustomEntries`
  : Custom elements to add to the discovery document. Defaults to empty.

## Authentication

`AuthenticationOptions`.

* `CookieAuthenticationScheme`
  : The cookie authentication scheme, configured by the host, that holds the interactive user's session. If not
    set, it is inferred from the host's default authentication scheme. Use it when the host's default scheme is a
    policy scheme (`AddPolicyScheme`). Defaults to `null`.
* `CookieLifetime`
  : The lifetime of the authentication cookie. Only applies to the server's own cookie handler. Defaults to 10 hours.
* `CookieSlidingExpiration`
  : Whether the authentication cookie has a sliding expiration. Only applies to the server's own cookie handler.
    Defaults to `false`.
* `CookieSameSiteMode`
  : The SameSite mode of the authentication cookie (`idsrv`) and the external cookie (`idsrv.external`).
    Defaults to `SameSiteMode.None`.
* `RequireAuthenticatedUserForSignOutMessage`
  : Whether the user must be authenticated for the end session endpoint to accept its parameters. Defaults to `false`.
* `CheckSessionCookieName`
  : The name of the cookie used by the check session endpoint. Defaults to `idsrv.session`.
* `CheckSessionCookieDomain`
  : The domain of the check session cookie. Defaults to `null`.
* `CheckSessionCookieSameSiteMode`
  : The SameSite mode of the check session cookie. Defaults to `SameSiteMode.None`.
* `RequireCspFrameSrcForSignout`
  : Emits `frame-src` CSP headers on the end session callback endpoint, which renders the iframes of front-channel
    logout. Defaults to `true`.
* `SendBackChannelLogoutNotificationsInBackground`
  : Sends back-channel logout notifications from a background service instead of before the sign-out response
    starts, so slow or unreachable clients do not delay the user's sign-out. Set it to `false` when a custom
    `IBackChannelLogoutService` needs the original HTTP request. Defaults to `true`.
* `EnforceAcrValues`
  : Makes the authorize endpoint require the request's `acr_values`: the session's `acr` claim must be one of
    them. When it is not, the user is sent to the login page once, which reads the values from the authorization
    context and can ask for a stronger sign-in (step-up). If the session still does not meet them on the way back,
    the client gets `unmet_authentication_requirements`. The `idp:` and `tenant:` values are not checked.
    Defaults to `false`: OpenID Connect Core makes `acr_values` a voluntary request, so the values only reach the
    login page. Turn it on when the login page sets the `acr` claim of the sessions it creates.

## Events

`EventsOptions` (namespace `MinApiLib.Oidc.Server.Core.Events`). Which events are sent to the registered event
sink. See [Events](../topics/events.md).

* `RaiseSuccessEvents`, `RaiseFailureEvents`, `RaiseInformationEvents`, `RaiseErrorEvents`
  : Raise each kind of event. Default to `false`.

## InputLengthRestrictions

`InputLengthRestrictions`. Maximum lengths of protocol parameters. Longer values are rejected.

| Property | Parameter | Default |
|---|---|---|
| `ClientId` | `client_id` | 100 |
| `ClientSecret` | client secret | 100 |
| `Scope` | `scope` | 300 |
| `RedirectUri` | `redirect_uri` | 400 |
| `Nonce` | `nonce` | 300 |
| `UiLocale` | `ui_locales` | 100 |
| `LoginHint` | `login_hint` | 100 |
| `LoginHintToken` | `login_hint_token` | 300 |
| `AcrValues` | `acr_values` | 300 |
| `State` | `state` | 4000 |
| `UserName` | user name of the password grant | 100 |
| `Password` | password of the password grant | 100 |
| `GrantType` | `grant_type` | 100 |
| `CspReport` | CSP reports | 2000 |
| `IdentityProvider` | external identity provider name | 100 |
| `ExternalError` | external identity provider errors | 100 |
| `AuthorizationCode` | authorization codes | 100 |
| `DeviceCode` | device codes | 100 |
| `RefreshToken` | refresh tokens | 100 |
| `TokenHandle` | reference token handles | 100 |
| `Jwt` | JWTs | 51200 |
| `ResourceIndicator` | `resource` (RFC 8707) | 512 |

The PKCE limits are read-only: `CodeChallengeMinLength` and `CodeVerifierMinLength` are 43,
`CodeChallengeMaxLength` and `CodeVerifierMaxLength` are 128.

## UserInteraction

`UserInteractionOptions` (namespace `MinApiLib.Oidc.Server.Features.Interaction`). Where the host's UI pages are and
which parameters they receive. Local URLs must start with a slash.

* `LoginUrl`, `LogoutUrl`
  : The login and logout pages. Default to `/account/login` and `/account/logout`. The server's session cookie
    handler uses them as its `LoginPath` and `LogoutPath` when they are local.
* `LoginReturnUrlParameter`
  : The name of the return URL parameter passed to the login page, and the cookie handler's `ReturnUrlParameter`.
    Defaults to `returnUrl`.
* `LogoutIdParameter`
  : The name of the logout message id parameter passed to the logout page. Defaults to `logoutId`.
* `ConsentUrl`
  : The consent page. Defaults to `/consent`.
* `ConsentReturnUrlParameter`
  : The name of the return URL parameter passed to the consent page. Defaults to `returnUrl`.
* `ErrorUrl`
  : The error page. Defaults to `/home/error`.
* `ErrorIdParameter`
  : The name of the error message id parameter passed to the error page. Defaults to `errorId`.
* `CustomRedirectReturnUrlParameter`
  : The name of the return URL parameter passed to a custom redirect from the authorize endpoint (see
    [prompt](../topics/prompt.md)). Defaults to `returnUrl`.
* `DeviceVerificationUrl`
  : The device verification page, returned to devices as `verification_uri`. Defaults to `/device`.
* `DeviceVerificationUserCodeParameter`
  : The name of the user code parameter in `verification_uri_complete`. Defaults to `userCode`.
* `CookieMessageThreshold`
  : Some interactions between the server and the UI pages pass state in a cookie (the pages above that take a
    message id). Browsers limit the number and size of cookies, so this sets the maximum number of message cookies
    of each type; the oldest are purged when it is reached. In effect, it is how many tabs a user can have open in
    the middle of such an interaction. Defaults to `2`.
* `PromptValuesSupported`
  : The `prompt` values the authorize endpoint accepts and discovery publishes as `prompt_values_supported`.
    Read-only collection. Defaults to `none`, `login`, `consent` and `select_account`. `AddPromptHandler` and
    `AddPromptRedirect` add their value here. A value added by hand needs a custom
    `IAuthorizeInteractionResponseGenerator` to act on it; otherwise it is accepted and ignored.

## Caching

`CachingOptions`. These durations are used by the caching decorators that `AddClientStoreCache<T>()`,
`AddResourceStoreCache<T>()` and `AddCorsPolicyCache<T>()` register. They have no effect without them.

* `ClientStoreExpiration`
  : How long client configuration loaded from the client store is cached. Defaults to 15 minutes.
* `ResourceStoreExpiration`
  : How long identity resources, API resources and API scopes loaded from the resource store are cached.
    Defaults to 15 minutes.
* `CorsExpiration`
  : How long the result of a CORS origin check is cached. Defaults to 15 minutes.

## CORS

`CorsOptions` (namespace `MinApiLib.Oidc.Server.Core.Cors`). The server answers CORS requests on some of its
endpoints, with ASP.NET Core's CORS implementation. See [CORS](../topics/cors.md).

* `CorsPolicyName`
  : The name of the CORS policy evaluated for CORS requests to the server. Defaults to `OidcServer`. The policy
    provider for it asks the registered `ICorsPolicyService`; to change which origins are allowed, implement
    `ICorsPolicyService` (`AddCorsPolicyService<T>()`).
* `CorsPaths`
  : The endpoints that answer CORS requests. Defaults to discovery, the JWKS, token, userinfo, revocation and
    pushed authorization.
* `PreflightCacheDuration`
  : The `Access-Control-Max-Age` value of preflight responses. Defaults to `null`, which sends no header.

## CSP (Content Security Policy)

`CspOptions`. The server emits CSP headers on the responses it renders.

* `Level`
  : The CSP level. Defaults to `CspLevel.Two`; set `CspLevel.One` to support older browsers.
* `AddDeprecatedHeader`
  : Also emits the older `X-Content-Security-Policy` header. Defaults to `true`.

## Validation

`ValidationOptions`.

* `InvalidRedirectUriPrefixes`
  : URI scheme prefixes that a client's redirect URIs may never use. Read-only collection. Defaults to
    `javascript:`, `file:`, `data:`, `mailto:`, `ftp:`, `blob:`, `about:`, `ssh:`, `tel:`, `view-source:`, `ws:`
    and `wss:`.

## Device Flow

`DeviceFlowOptions` (namespace `MinApiLib.Oidc.Server.Features.DeviceFlow`).

* `DefaultUserCodeType`
  : The user code type, unless the client sets `UserCodeType`. Defaults to `Numeric`, a 9-digit code.
* `Interval`
  : The minimum polling interval on the token endpoint, in seconds. Defaults to `5`.
* `UserCodeMaxFailedAttempts`
  : The maximum number of failed user code lookups for one user (or one client IP address when nobody is signed
    in) within `UserCodeFailedAttemptsWindow`. Further lookups fail until the window passes, which limits brute
    forcing of user codes (RFC 8628 section 5.1). `0` disables the limit. Defaults to `5`.
* `UserCodeFailedAttemptsWindow`
  : The window in which failed user code lookups are counted. Defaults to 5 minutes.

## Logging

`LoggingOptions` (namespace `MinApiLib.Oidc.Server.Core.Logging`). Parameters whose values are masked when a request
is logged.

* `TokenRequestSensitiveValuesFilter`
  : Token request parameters. Defaults to `client_secret`, `password`, `client_assertion`, `refresh_token`,
    `device_code`, `code` and `code_verifier`.
* `AuthorizeRequestSensitiveValuesFilter`
  : Authorize request parameters. Defaults to `id_token_hint` and `login_hint_token`.

## Mutual TLS

`MutualTlsOptions` (namespace `MinApiLib.Oidc.Server.Core.Hosting`). See [Mutual TLS](../topics/mtls.md).

* `Enabled`
  : Enables mutual TLS support. Defaults to `false`.
* `ClientCertificateAuthenticationScheme`
  : The name of the authentication handler for X.509 client certificates. Defaults to `Certificate`.
* `DomainName`
  : A separate domain for the mutual TLS endpoints. A name without dots is a subdomain (`mtls` gives
    `mtls.login.example.com`); a name with dots is a full domain (`mtls.example.com`), and then `IssuerUri` must
    be set. If not set, the endpoints use the `/connect/mtls/...` paths. Defaults to `null`.
* `AlwaysEmitConfirmationClaim`
  : Emits a `cnf` claim in access tokens whenever a client certificate is present. Normally it is only emitted
    when the client authenticated with the certificate. Defaults to `false`.

## Token Validation

`TokenValidationOptions` (namespace `MinApiLib.Oidc.Server.Core.TokenServices`). How the server validates the
tokens it receives (at the userinfo, introspection and nonce endpoints, for example).

* `ValidateIssuer`
  : Whether the issuer is validated. Defaults to `true`.
* `ValidIssuers`
  : Issuers accepted besides the current one. Defaults to `null`.

## Pushed Authorization

`PushedAuthorizationOptions` (namespace `MinApiLib.Oidc.Server.Features.PushedAuthorization`). See
[Pushed Authorization Requests](../topics/par.md).

* `Required`
  : Requires every client to use pushed authorization requests (RFC 9126). A single client can require them with
    `Client.RequirePushedAuthorization`. Defaults to `false`.
* `Lifetime`
  : The lifetime of a pushed authorization request, in seconds. It must cover the login and consent that follow
    the redirect to the authorize endpoint. Defaults to `600`.

## DPoP

`DPoPOptions` (namespace `MinApiLib.Oidc.Server.Core.DPoP`). See [Proof-of-Possession](../topics/pop.md).

* `ProofTokenValidityDuration`
  : How long after its `iat` a proof is accepted. Defaults to 1 minute.
* `ClockSkew`
  : The tolerated difference between the client and server clocks. Defaults to 1 minute.
* `SupportedSigningAlgorithms`
  : The signing algorithms accepted for proofs. Only asymmetric algorithms are allowed. Defaults to RS256, RS384,
    RS512, PS256, PS384, PS512, ES256, ES384 and ES512.

## Key Management

`KeyManagementOptions` (namespace `MinApiLib.Oidc.Server.Core.Keys`). Automatic creation and rotation of signing
keys. See [Automatic key management](../topics/crypto.md#automatic-key-management).

* `Enabled`
  : Creates and rotates signing keys automatically. Defaults to `false`, so signing keys come only from
    `AddSigningCredential` and similar calls. When enabled, automatic keys sign tokens for `SigningAlgorithms`;
    static credentials are still published for validation and sign for any other algorithm.
* `SigningAlgorithms`
  : The algorithms automatic keys are created for. The first one is the default. Defaults to RS256.
* `RotationInterval`
  : How long a key signs tokens, counted from its creation. Defaults to 90 days.
* `PropagationTime`
  : How long a new key is published in the JWKS before it signs tokens, so clients and APIs that cache the key set
    pick it up first. Defaults to 14 days.
* `RetentionDuration`
  : How long a retired key stays published to validate the tokens it signed. Defaults to 14 days.
* `DeleteRetiredKeys`
  : Deletes keys past their retention from the store. Defaults to `true`.
* `KeyCacheDuration`
  : How long keys loaded from the store are cached in memory. Other instances see a new key within this time, so
    it must be shorter than `PropagationTime`. Defaults to 1 hour.
* `DataProtectKeys`
  : Encrypts private keys with ASP.NET Core data protection before they are stored. Defaults to `true`.
* `KeyPath`
  : The directory of the default file system key store (`FileSystemSigningKeyStore`). Defaults to a `keys` folder
    in the current directory. `AddMongoStores` replaces the file system store with a MongoDB one.

## Middleware options

`UseOidcServer` takes an optional `OidcServerMiddlewareOptions` (namespace `Microsoft.AspNetCore.Builder`):

* `AuthenticationMiddleware`
  : The callback that adds the authentication middleware. Defaults to `app => app.UseAuthentication()`.

[[go back to index]](../index.md#documentation-structure)
