# MinApiLib.Oidc documentation

MinApiLib.Oidc is OpenID Connect and OAuth 2.0 for ASP.NET Core: a server, `HttpClient` handlers that get and send
access tokens, and DPoP (RFC 9449) support for APIs. It is built on [MinApiLib](https://github.com/fernandoescolar/MinApiLib)
and targets `net10.0` and `net11.0`.

The server, `MinApiLib.Oidc.Server`, derives from an IdentityServer fork of
[IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 4.1.2, and this documentation from its
documentation. The fork is kept, archived, on the `archived/identity-server` branch.

- [Documentation structure](#documentation-structure)
- [What it does](#what-it-does)

## Documentation structure

- INTRODUCTION
  - [The Big Picture](intro/big_picture.md)
  - [Terminology](intro/terminology.md)
  - [Supported Specifications](intro/specs.md)
  - [Packages and Builds](intro/packaging.md)
  - [Contributing](intro/contributing.md)
- QUICKSTARTS
  - [Overview](quickstarts/0_overview.md)
  - [Protecting an API using Client Credentials](quickstarts/1_client_credentials.md)
  - [Interactive Applications with ASP.NET Core](quickstarts/2_interactive_aspnetcore.md)
  - [ASP.NET Core and API access](quickstarts/3_aspnetcore_and_apis.md)
  - [Adding a JavaScript client](quickstarts/4_javascript_client.md)
  - [Using MongoDB for configuration and operational data](quickstarts/5_mongodb.md)
  - [Using ASP.NET Core Identity and passkeys](quickstarts/6_aspnet_identity.md)
- TOPICS
  - [Startup](topics/startup.md)
  - [Defining Resources](topics/resources.md)
  - [Defining Clients](topics/clients.md)
  - [Sign-in](topics/signin.md)
  - [Custom prompt values](topics/prompt.md)
  - [Sign-in with External Identity Providers](topics/signin_external_providers.md)
  - [Windows Authentication](topics/windows.md)
  - [Sign-out](topics/signout.md)
  - [Sign-out of External Identity Providers](topics/signout_external_providers.md)
  - [Federated Sign-out](topics/signout_federated.md)
  - [Federation Gateway](topics/federation_gateway.md)
  - [Consent](topics/consent.md)
  - [Protecting APIs](topics/apis.md)
  - [Deployment](topics/deployment.md)
  - [Logging](topics/logging.md)
  - [Events](topics/events.md)
  - [Cryptography, Keys and HTTPS](topics/crypto.md)
  - [Grant Types](topics/grant_types.md)
  - [Client Authentication](topics/client_authentication.md)
  - [Extension Grants](topics/extension_grants.md)
  - [Resource Owner Password Validation](topics/resource_owner.md)
  - [Refresh Tokens](topics/refresh_tokens.md)
  - [Reference Tokens](topics/reference_tokens.md)
  - [Persisted Grants](topics/persisted_grants.md)
  - [Proof-of-Possession Access Tokens](topics/pop.md)
  - [Pushed Authorization Requests](topics/par.md)
  - [Mutual TLS](topics/mtls.md)
  - [Authorize Request Objects](topics/request_object.md)
  - [Custom Token Request Validation and Issuance](topics/custom_token_request_validation.md)
  - [CORS](topics/cors.md)
  - [Discovery](topics/discovery.md)
  - [Adding more API Endpoints](topics/add_apis.md)
  - [Adding new Protocols](topics/add_protocols.md)
  - [Tools](topics/tools.md)
- ENDPOINTS
  - [Discovery Endpoint](endpoints/discovery.md)
  - [Authorize Endpoint](endpoints/authorize.md)
  - [Token Endpoint](endpoints/token.md)
  - [UserInfo Endpoint](endpoints/userinfo.md)
  - [Device Authorization Endpoint](endpoints/device_authorization.md)
  - [Introspection Endpoint](endpoints/introspection.md)
  - [Revocation Endpoint](endpoints/revocation.md)
  - [End Session Endpoint](endpoints/endsession.md)
  - [Nonce Endpoint](endpoints/nonce.md)
- REFERENCE
  - [Server Options](reference/options.md)
  - [Identity Resource](reference/identity_resource.md)
  - [API Scope](reference/api_scope.md)
  - [API Resource](reference/api_resource.md)
  - [Client](reference/client.md)
  - [GrantValidationResult](reference/grant_validation_result.md)
  - [Profile Service](reference/profileservice.md)
  - [Interaction Service](reference/interactionservice.md)
  - [Device Flow Interaction Service](reference/deviceflow_interactionservice.md)
  - [Nonce Authorization Pattern](reference/nonce_pattern.md)

Each package also has a README with its API:
[Server](../src/MinApiLib.Oidc.Server/README.md), [Server.MongoDB](../src/MinApiLib.Oidc.Server.MongoDB/README.md),
[DPoP](../src/MinApiLib.Oidc.DPoP/README.md), [Http](../src/MinApiLib.Oidc.Http/README.md),
[Authentication](../src/MinApiLib.Oidc.Authentication/README.md) and [JwtBearer](../src/MinApiLib.Oidc.JwtBearer/README.md).
[samples/](../samples/README.md) has a server, a web client, an API and a worker built with them.

## What it does

### Authentication as a service

One place for the sign-in of all your applications (web, native, mobile, services). The server implements OpenID
Connect; you write the login page, so it can use passwords, passkeys, external providers or anything else.

### Single sign-on and sign-out

One session across your applications, with front-channel and back-channel logout.

### Access control for APIs

Access tokens for APIs, for every kind of client: services (client credentials), web applications, SPAs and native
apps. Tokens can be bound to a key with DPoP or mutual TLS, so a stolen token is useless without the key.

### Federation gateway

Sign in with external identity providers such as Microsoft Entra ID or Google, without your applications knowing
the details of each one.

### Clients and APIs too

`MinApiLib.Oidc.Http` and `MinApiLib.Oidc.Authentication` get, cache, refresh and send access tokens from your
applications, as bearer or DPoP-bound tokens. `MinApiLib.Oidc.JwtBearer` adds DPoP to ASP.NET Core JWT bearer
authentication in APIs. They work with any OpenID Connect server.

### Open to customization

The server is a library, not a boxed product: its services are interfaces you can replace, and its endpoints are
ordinary ASP.NET Core endpoints you can add policies to.
