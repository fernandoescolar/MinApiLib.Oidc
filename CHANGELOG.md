# Changelog

This file summarises how MinApiLib.Oidc differs from
[IdentityServer4 4.1.2](https://github.com/IdentityServer/IdentityServer4/tree/4.1.2), the last upstream release.
`MinApiLib.Oidc.Server` derives from it through an IdentityServer fork, which this repository keeps, archived, on the
`archived/identity-server` branch; the protocol changes below come from that fork. No version has been published
yet, so every change is listed under *Unreleased*.

## [Unreleased]

### Moving from IdentityServer4

Read this before you move a host from IdentityServer4 4.1.2 to `MinApiLib.Oidc.Server`:

- Rename the API. `AddIdentityServer()`, `UseIdentityServer()` and `IdentityServerOptions` become `AddOidcServer()`,
  `UseOidcServer()` and `OidcServerOptions`, and the `IdentityServer4.*` namespaces become
  `MinApiLib.Oidc.Server.Features.*` and `MinApiLib.Oidc.Server.Core.*`, by feature. The
  [server README](src/MinApiLib.Oidc.Server/README.md#differences-from-the-identityserver-fork) maps every name.
- Replace `using IdentityModel;` with `using MinApiLib.Oidc.Server.Core.IdentityModel;`. The server does not depend
  on the `IdentityModel` package.
- Rename the local API scope. A client that calls the server's own APIs with the `IdentityServerApi` scope needs
  `OidcServerApi`; the other values that carried the product name are listed in the server README.
- Map the endpoints. Call `app.MapOidcServer()` as well as `app.UseOidcServer()`. Custom endpoints are MinApiLib
  endpoint records instead of `IEndpointHandler`.
- Bring your own ASP.NET Core Identity integration. There is no `AddAspNetIdentity<TUser>()`: implement
  `IProfileService` (and `IResourceOwnerPasswordValidator`, if a client still uses the password grant) over your user
  store. The sample server signs users in with ASP.NET Core Identity.
- Opt in to deprecated flows. Clients that use the implicit flow, access tokens from the authorize endpoint, or the
  password grant need the new client flags (see [Deprecated flows](#deprecated-flows)).
- Replace `ISystemClock` with `TimeProvider`.
- Check persisted grants. They are serialized with System.Text.Json instead of Newtonsoft.Json; verify that grants
  stored by IdentityServer4 still deserialize, or let them expire, before switching a production store.
- Fix invalid options. Startup fails on inconsistent options (see [Configuration](#configuration)).
- Review [Changed behaviour](#changed-behaviour) and [API changes](#api-changes).

### Packages

- **Target frameworks.**
  - Targets `net10.0` and `net11.0`, with C# `latest`.
  - Nullable reference types are enabled in every library.
  - The SDK is pinned in `global.json`.
  - The package id prefix is configurable with `PackageIdPrefix`.
  - There are no Entity Framework packages, quickstart UI or templates.
- **Dependencies.**
  - Microsoft.IdentityModel 8.x replaces the vulnerable versions IdentityServer4 pinned.
  - System.Text.Json replaces Newtonsoft.Json.
  - `TimeProvider` replaces `ISystemClock`.
  - The protocol constants and helpers the server needs (`OidcConstants`, `JwtClaimTypes`, `CryptoRandom`, ...)
    are derived from IdentityModel 7.0.0 (Apache-2.0) and live in the server, in the
    `MinApiLib.Oidc.Server.Core.IdentityModel` namespace, so hosts can still reference `IdentityModel` or
    `Duende.IdentityModel` for client code.
- **Builds and packaging.**
  - Builds are reproducible.
  - Versions come from git tags (MinVer).
  - Packages ship Source Link with `.snupkg` symbols, XML documentation and a README.
  - `LICENSE` and `NOTICE` carry the Apache-2.0 attribution to Brock Allen and Dominick Baier.
  - Assemblies are not strong-name signed.
- **`MinApiLib.Oidc.Server`.** The server, in one package, built on [MinApiLib](https://github.com/fernandoescolar/MinApiLib)
  endpoint records and attribute-based dependency injection (see [Hosting](#hosting)).
  - The protocol behaviour is that of the IdentityServer fork, whose integration test suite runs against it in
    `tests/MinApiLib.Oidc.Server.IntegrationTests`.
  - `options.Authentication.EnforceAcrValues`, off by default, makes `acr_values` required: a session whose `acr`
    is not one of them goes back to the login page once (step-up), and then the client gets
    `unmet_authentication_requirements`, which is now returned to the client's redirect URI.
- **`MinApiLib.Oidc.Server.MongoDB`.** MongoDB stores for clients, resources, CORS origins, persisted
  grants, device codes and the signing keys of automatic key management, with TTL indexes that expire grants and
  device codes. Its tests run against MongoDB in Docker (Testcontainers).
- **Client packages.** `HttpClient` handlers that send access tokens, for any OpenID Connect server, as bearer
  tokens or DPoP-bound, handling `DPoP-Nonce`:
  - `MinApiLib.Oidc.Http`, without ASP.NET Core: client credentials tokens (`AddClientCredentialsTokens`,
    `AddClientCredentialsTokenHandler`), cached and renewed, and the `AccessTokenHandler` and `TokenEndpointClient`
    to build other handlers on.
  - `MinApiLib.Oidc.Authentication`: the signed-in user's tokens (`AddOpenIdConnectTokens`,
    `AddUserAccessTokenHandler`). It refreshes them, updates the session and binds the authorization code to the
    DPoP key (`dpop_jkt`).
- **`MinApiLib.Oidc.DPoP`.** DPoP keys, proofs, RFC 7638 thumbprints, nonces and the names of RFC 9449,
  shared by the client packages and `MinApiLib.Oidc.Server`, and `DPoPProofValidator` with a replay cache for
  servers that validate proofs. The server's `DefaultDPoPProofValidator` and `MinApiLib.Oidc.JwtBearer` both use it,
  so the token endpoint and the APIs check proofs the same way: `htu` matches with scheme and host case-insensitive
  and the path exact.
- **`MinApiLib.Oidc.JwtBearer`.** DPoP for APIs on ASP.NET Core JWT bearer authentication:
  `AddJwtBearer().AddDPoP()` accepts `Authorization: DPoP`, validates the proof against `cnf.jkt`, rejects DPoP-bound
  tokens sent as bearer tokens, can require DPoP-bound tokens only or API-issued nonces, and announces both schemes in
  its challenges. The API sample uses it.
- **Samples.** `samples/` has a server, a web client, an API and a worker that show the code flow, client
  credentials and API calls with bearer and DPoP-bound access tokens, in memory or on MongoDB. The server's users
  are in ASP.NET Core Identity and sign in with a password or a passkey; a client that asks for `acr_values=phr` gets
  a passkey-only sign-in, and the tokens carry `amr` and `acr`.

### Hosting

- **Endpoint routing.** Every protocol endpoint is a MinApiLib endpoint record for the HTTP methods it supports,
  mapped by `MapOidcServer()` with its `/connect/mtls/...` alias. Other methods get a routing 405.
  - `UseOidcServer()` adds the middleware: session cookie, CORS, authentication, mutual TLS and logout
    notifications.
  - A host that forgets `MapOidcServer()` fails with a clear `InvalidOperationException`.
- **Per-endpoint policies.** `MapOidcServer()` returns a conventions builder, with `ForEndpoint(name)` for one
  endpoint, so you can apply ASP.NET Core policies such as rate limiting or output caching.
  `OidcServerConstants.EndpointNames` is public.
- **Endpoint results.** Every endpoint result is an ASP.NET Core `IResult`.
- **Back-channel logout.** Notifications are sent from a background service (`Channel<T>`), so slow clients no longer
  delay the user's sign-out. Set `Authentication.SendBackChannelLogoutNotificationsInBackground = false` to send them
  inline.
- **Issuer service.** The new `IIssuerNameService` resolves the issuer. With `IssuerUri` configured, tokens can be
  created outside an HTTP request.

### Added

- **Pushed authorization requests (RFC 9126).**
  - `/connect/par` requires client authentication and issues single-use request URIs.
  - It applies the lessons of GHSA-mxv6-xwqj-ww2p: the pushed request is bound to the authenticated client, it
    accepts no unregistered redirect URIs, and it is consumed on success and on error.
  - Configured with `Client.RequirePushedAuthorization` and `PushedAuthorizationOptions.Required`.
- **DPoP (RFC 9449).**
  - Full proof validation and `cnf.jkt` binding. Access tokens have type `DPoP`.
  - `dpop_jkt` at the authorize endpoint and proofs at PAR bind the authorization code to the key.
  - Refresh tokens of public clients are bound to the key.
  - `Client.RequireDPoP`.
  - The userinfo endpoint and the Local API handler enforce the binding (lesson of CVE-2024-49755).
- **Resource indicators (RFC 8707).**
  - The `resource` parameter is accepted at the authorize and token endpoints.
  - The access token audience is narrowed to the requested APIs.
  - `ApiResource.RequireResourceIndicator`.
- **Automatic signing key rotation.**
  - Keys are created, announced, rotated, retired and deleted on a schedule.
  - Private keys are protected with ASP.NET Core data protection.
  - Keys are stored through `ISigningKeyStore`, on the file system by default.
  - Off by default: `KeyManagementOptions.Enabled`.
- **Issuer in authorization responses (RFC 9207).** On by default
  (`EmitIssuerIdentificationResponseParameter`) and advertised in discovery.
- **`client_id` at the end session endpoint (RP-Initiated Logout 1.0).**
  - It validates `post_logout_redirect_uri` without an `id_token_hint`.
  - The sign-out prompt is skipped only with a valid `id_token_hint` (`LogoutRequest.ClientAuthenticated`).
- **Custom `prompt` values.**
  - `UserInteraction.PromptValuesSupported` lists the accepted values; discovery publishes them as
    `prompt_values_supported`.
  - `AddPromptRedirect(prompt, url)` sends the user to a page. `AddPromptHandler<T>(prompt)` registers an
    `IAuthorizePromptHandler` for full control.
  - Use it for the OpenID Connect `create` value or an application-specific one such as `register`.
- **Nonce pattern.**
  - `/connect/nonce`, the `urn:auth:nonce` grant and the `login_hint_token` parameter let a client turn an access
    token into an interactive sign-in.
  - Nonces are single use, expire, and respect the target client's login policies.
- **Mutual TLS.**
  - `X509ThumbprintSecretValidator` also accepts SHA-256 thumbprints.
  - `StrictRedirectUriValidatorAppAuth` follows RFC 8252 for native apps.
- **Device flow.** `user_code` lookups are rate limited per user or IP (`DeviceFlowOptions.UserCodeMaxFailedAttempts`).
- **Discovery.** It publishes `request_object_signing_alg_values_supported`,
  `token_endpoint_auth_signing_alg_values_supported`, `pushed_authorization_request_endpoint` and
  `dpop_signing_alg_values_supported`.

### Deprecated flows

OAuth 2.1 and RFC 9700 deprecate these flows. They remain available for existing clients behind client flags that
default to `false`. `AllowImplicitFlow` and `AllowResourceOwnerPasswordFlow` are new: IdentityServer4 only needed the
grant type in `AllowedGrantTypes`. A client that lists the grant type without its flag fails configuration validation, and the
endpoints check the flags again on every request. The deprecated response and grant types are not published in
discovery.

| Flow | Client configuration |
|---|---|
| Implicit (`id_token`, `id_token token`, `token`) | `GrantTypes.Implicit` and `AllowImplicitFlow = true` |
| Access tokens from the authorize endpoint (`token`, `id_token token`, `code token`, `code id_token token`) | `AllowAccessTokensViaBrowser = true` (as in IdentityServer4) |
| Resource owner password credentials | `GrantTypes.ResourceOwnerPassword`, `AllowResourceOwnerPasswordFlow = true`, and an `IResourceOwnerPasswordValidator` (`AddResourceOwnerValidator<T>()`) |
| PKCE `plain` | `AllowPlainTextPkce = true` (unchanged) |

An implicit request with the `openid` scope must carry a `nonce`. Without an `IResourceOwnerPasswordValidator`, the
password grant answers `unsupported_grant_type`. The `password` parameter is removed from the logs by default.

### Security fixes

- **Inherited from IdentityServer4.**
  - Open redirect in `IsLocalUrl` (CVE-2024-39694).
  - The error page no longer receives the client redirect URI for unsafe errors (upstream PR #1933).
  - `client_credentials` clients must have a secret (PR #1796).
  - The AppAuth validator checks post-logout URIs against `PostLogoutRedirectUris` (PR #619).
  - The authorization parameters message store now deletes its entries.
- **Mutual TLS.** The certificate is trusted only on the mTLS endpoints, after the certificate scheme authenticated
  it.
- **Refresh tokens.**
  - Reusing a rotated refresh token revokes the subject's refresh tokens for that client.
  - Scopes are revalidated on refresh.
  - A narrower `scope` is accepted.
  - Each access token gets a new `jti`.
- **Authorization codes.** A redeemed code is kept as consumed. A second redemption is rejected and revokes the
  tokens issued from it.
- **JWT-secured authorization requests.** `request_uri` must be registered in `Client.RequestUris`. Fetching it
  follows no redirects and connects only to public IP addresses.
- **Redirect URIs.** Path and query are compared exactly, fragments are rejected, and loopback rules follow
  RFC 8252.
- **Token types.**
  - Logout tokens use `typ: logout+jwt`.
  - Introspection of reference tokens keeps `cnf`.
  - Access tokens always carry an audience (RFC 9068).
- **Client authentication.** Using two client authentication methods at once is rejected.
- **Input lengths.** `state` has a length limit.
- **Logging.** Sensitive values are filtered from the logs: codes, verifiers, secrets, refresh tokens, passwords and
  `login_hint_token`.
- **Startup warnings.** Outside Development, startup warns about a missing `IssuerUri`, the in-memory grant store and
  the developer signing credential.

### Changed behaviour

- **Unknown `prompt` values.** They return `invalid_request` unless registered as custom prompt values.
- **Redirects after POST.** They use HTTP 303 instead of 302.
- **Authorization responses.** They include `iss`.
- **Discovery.** `code_challenge_methods_supported` lists only `S256`.
- **Refresh token reuse.** It signs the user out of that client: the subject's refresh tokens for the client are
  revoked. A refresh fails with `invalid_scope` if the client lost access to a granted scope.
- **Mutual TLS clients.** A client that calls `/connect/token` directly no longer authenticates with its
  certificate. It must use the mTLS endpoint or domain.
- **Back-channel logout.** Notifications arrive shortly after the sign-out response instead of before it.
- **Sign-out prompt.** `LogoutRequest.ShowSignoutPrompt` is `true` unless the request has a valid `id_token_hint`.
- **Endpoints and tokens.**
  - The PAR endpoint is enabled by default.
  - A client that sends a `DPoP` header receives `DPoP` tokens.
  - `IReplayCache` is always registered.
- **Disabled endpoints.** A disabled endpoint answers 404.
- **Unsupported methods.** A request with a method the endpoint does not support gets a routing 405 with an `Allow`
  header, instead of a 405 or a 400 `invalid_request` from the endpoint.
- **End session POST.** A `POST` to `/connect/endsession` without a form body gets 415, like the other form
  endpoints, instead of failing while reading the form.
- **Device flow denial.** `IDeviceFlowInteractionService.HandleRequestAsync` honours `ConsentResponse.Error`: the
  device gets `access_denied` and the result has `IsAccessDenied`. Before, a response with an error and scopes
  authorized the device. As at the authorize endpoint, leaving out a requested `Required` scope also denies it, and
  with `RememberConsent` and a client that allows it, the consent is stored.
- **In-memory stores.** `InMemoryPersistedGrantStore` and `InMemoryDeviceFlowStore` remove expired entries when a
  new one is stored, at most once a minute. Both take an optional `TimeProvider`.
- **Return URL parsers.** An `IReturnUrlParser` registered before `AddOidcServer()` is added next to the OpenID
  Connect parser instead of replacing it.
- **Device flow limit.** `DeviceFlowOptions.UserCodeMaxFailedAttempts` can be `0`, which disables the limit, as its
  documentation said. The options validator rejected it.
- **Null handling.** Places that could throw `NullReferenceException` now throw `InvalidOperationException` with a
  message, for example when a UI URL is not configured.
- **Log events.** Log messages have new event ids. Interaction errors are logged as "Interaction error: …".

### Configuration

- **Options validation.** `OidcServerOptions` is validated at startup (`IValidateOptions` + `ValidateOnStart`).
  Each of these stops the host:
  - an issuer that is not an absolute URL;
  - non-positive lifetimes or limits;
  - symmetric DPoP algorithms;
  - inconsistent key management settings.
- **Explicit `TimeProvider`.** `TimeProvider.System` is registered explicitly.
- **Nonce endpoint.** `EnableNonceEndpoint = false` disables it.

### API changes

Beyond the renames in the [server README](src/MinApiLib.Oidc.Server/README.md#differences-from-the-identityserver-fork):

- **Removed.**
  - `IEndpointRouter`, `IEndpointHandler` and `AddEndpoint<T>()`: endpoints are MinApiLib endpoint records.
  - `Client.PairWiseSubjectSalt` and `SubjectTypes`, because pairwise subjects were never implemented.
  - The obsolete `PrincipalExtensions.GetName`.
- **Constructors.**
  - Several constructors take `IIssuerNameService` instead of `IHttpContextAccessor`: `OidcServerTools`,
    `JwtRequestValidator`, `PrivateKeyJwtSecretValidator`, `LogoutNotificationService` and `DefaultTokenService`.
  - `EndSessionRequestValidator` takes `IClientStore`.
- **Interfaces.**
  - Store interfaces return `Task<T?>` for lookups.
  - `ProfileDataRequestContext.RequestedResources` and `.ValidatedRequest` are nullable.
  - `IClaimsService.GetAccessTokenClaimsAsync` accepts a null subject.
  - Some message store members accept or return `null`.
- **New contract members.** `IRefreshTokenService.CreateRefreshTokenAsync` takes the authorized resource indicators.
  `IAuthorizationCodeStore.ConsumeAuthorizationCodeAsync`, `IAuthorizeRequestValidator` and `ITokenRequestValidator`
  gained members with default implementations.

### Internal changes

- **Vertical slices.** The code is organized by feature (`Features/Tokens`, `Features/Authorize`, ...) with a shared
  `Core`, and the default services are registered with MinApiLib's `[RegisterInIServiceCollection]` attribute.
- **Token request validation.** `TokenRequestValidator` is split into one validator per grant, with no per-request
  state in fields.
- **Decorators.** Keyed services replace the reflection-based decorators.
- **Logging.** All library logging uses source-generated `[LoggerMessage]` methods.
- **Serialization.** Persisted grants use source-generated System.Text.Json contracts. A test checks that the output
  matches the reflection serializer.
- **Collections and cryptography.**
  - `FrozenSet` and `FrozenDictionary` for lookup tables.
  - `SHA256.HashData`.
  - In-box `Base64Url`.
  - `CryptographicOperations.FixedTimeEquals`.
- **Build.** Warnings are errors, the .NET analyzers run in `Recommended` mode, and code style is enforced in the
  build.
- **Removed code.** Dead code and empty or copied comments were removed.

### Tests and tooling

- **Test stack.**
  - Tests run on xUnit v3 with Microsoft.Testing.Platform.
  - Assertions use xUnit's `Assert`; FluentAssertions was removed.
  - The server's integration tests carry a small test-only protocol client ported from IdentityModel 7.0.0.
  - The client packages and `MinApiLib.Oidc.JwtBearer` are tested against a real server, in memory.
- **Hosting tests.** The integration suite runs through endpoint routing, and the hosts are disposed after each
  test.
- **Snapshots.** Snapshot tests pin the discovery document and the shape of the access token.
- **Mutation testing.** Stryker.NET is installed as a local tool and configured in `stryker-config.json`.
- **New tests.** New tests cover every security fix and new feature. Each fix was checked by reverting it and seeing
  a test fail.
