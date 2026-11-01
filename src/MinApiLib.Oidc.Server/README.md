# MinApiLib.Oidc.Server

An OpenID Connect and OAuth 2.0 server for ASP.NET Core built on [MinApiLib](https://github.com/fernandoescolar/MinApiLib):

- every protocol endpoint is a MinApiLib endpoint record, mapped with `MapEndpoints`;
- the default services are registered with MinApiLib's `[RegisterInIServiceCollection]` attribute and `AddAssembly`.

It is a new implementation derived from the IdentityServer fork of IdentityServer4 4.1.2 (Apache-2.0), which this
repository keeps, archived, on the `archived/identity-server` branch. It is a single package with no dependency on
any IdentityServer package. It
depends only on ASP.NET Core, `Microsoft.IdentityModel.*`, `MinApiLib.Endpoints`, `MinApiLib.DependencyInjection` and
[MinApiLib.Oidc.DPoP](../MinApiLib.Oidc.DPoP/README.md), whose DPoP names and access token hash it shares with the clients.

It targets `net10.0` and `net11.0`. [MinApiLib.Oidc.Server.MongoDB](../MinApiLib.Oidc.Server.MongoDB/README.md) stores
its configuration and operational data in MongoDB, [MinApiLib.Oidc.Http](../MinApiLib.Oidc.Http/README.md)
and [MinApiLib.Oidc.Authentication](../MinApiLib.Oidc.Authentication/README.md) get and send access tokens, bearer or
DPoP-bound, from client applications, and [samples/](../../samples/README.md)
has a server, a web client, an API and a worker built with them.

## Getting started

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOidcServer(options => options.IssuerUri = "https://login.example.com")
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddDeveloperSigningCredential();

var app = builder.Build();

app.UseRouting();
app.UseOidcServer();      // session cookie, CORS, authentication, mutual TLS, logout notifications
app.UseAuthorization();

app.MapOidcServer()       // the protocol endpoints
   .ForEndpoint(OidcServerConstants.EndpointNames.Token).RequireRateLimiting("token");

app.Run();
```

`MapOidcServer()` is required. Without it, the first request fails with an error that says so. It returns a
conventions builder: conventions added to it apply to every protocol endpoint, and `ForEndpoint(name)` targets
one endpoint together with its mTLS alias.

## Layout

The code is organized in vertical slices. Each protocol feature has its own folder and namespace with
everything it needs:

- its endpoint records and their routes;
- its extension points and their default implementations;
- its request and response models and its results;
- its events and its log messages.

What several features share lives in `Core`.

| Folder (namespace `MinApiLib.Oidc.Server.*`) | Contents |
|---|---|
| `Features/Authorize` | Authorize endpoint (GET, POST, callback), request validation, request objects, prompts, interaction and response generators, redirect URI validation |
| `Features/PushedAuthorization` | PAR endpoint and its store (RFC 9126) |
| `Features/Tokens` | Token endpoint, token request validation, custom validators, response generator |
| `Features/Tokens/Grants` | One validator per grant: authorization code, client credentials, refresh token, device code, password, extension grants |
| `Features/DeviceFlow` | Device authorization endpoint, device and user codes, throttling, device flow interaction (RFC 8628) |
| `Features/Introspection`, `Features/Revocation`, `Features/UserInfo`, `Features/Discovery`, `Features/Nonce` | One endpoint each, with its validator, response generator and results |
| `Features/Session` | End session, its callback, check session, user session, front-channel and back-channel logout, federated sign-out |
| `Features/Interaction` | `IOidcServerInteractionService` for the login and consent UI: consent, messages, return URLs, grants |
| `Features/LocalApi` | Local API authentication handler |
| `Core/Configuration` | `AddOidcServer`, `UseOidcServer`, `MapOidcServer`, the builder and `OidcServerOptions` |
| `Core/Hosting`, `Core/Cors` | Middleware, the shared endpoint behaviour (`AsOidcServerEndpoint`), CORS |
| `Core/Models`, `Core/Stores` | Clients, resources, grants and tokens, and their stores |
| `Core/TokenServices`, `Core/Keys` | Token creation and validation, claims, signing keys |
| `Core/ClientAuthentication`, `Core/Clients`, `Core/ResourceValidation`, `Core/DPoP`, `Core/Users` | Client secrets and assertions, client configuration, scopes and resource indicators, DPoP, profile service |
| `Core/Events`, `Core/Logging`, `Core/Infrastructure`, `Core/Extensions`, `Core/IdentityModel` | Cross-cutting services and protocol constants |

`OidcServerConstants` stays in the root namespace. The registration API (`AddOidcServer`, `UseOidcServer`,
`MapOidcServer`) lives in the `Microsoft.Extensions.DependencyInjection` and `Microsoft.AspNetCore.Builder`
namespaces. A host usually imports `MinApiLib.Oidc.Server.Core.Models`, plus the namespace of each feature it
extends: for example, `MinApiLib.Oidc.Server.Features.Tokens` to implement `ICustomTokenRequestValidator`.

## Endpoints

Each endpoint is a MinApiLib record for the HTTP methods the protocol defines for it. Most derive from
`PostHandlerAsync<TRequest>` or `GetHandlerAsync<TRequest>`. Its `Request` is a record struct bound with
`[AsParameters]`. It carries the `HttpContext` and, marked `[FromServices]`, the services the endpoint needs for
that request. MinApiLib creates the record once at startup, so it holds no services itself:

```csharp
internal abstract record TokenEndpoint(string Path) : PostHandlerAsync<TokenEndpoint.Request>(Path)
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientSecretValidator ClientValidator,
        [FromServices] ITokenRequestValidator RequestValidator,
        [FromServices] ITokenResponseGenerator ResponseGenerator,
        [FromServices] IEventService Events,
        [FromServices] IDPoPProofValidator DPoPProofValidator,
        [FromServices] ILogger<TokenEndpoint> Logger);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint(EndpointNames.Token, Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

The routes are sealed records at the end of each endpoint file. There is one per path and method, plus the
`/connect/mtls/...` alias of every `connect/` route:

| Endpoint | Routes |
|---|---|
| Authorize | `GET` and `POST /connect/authorize` (one record each), `GET /connect/authorize/callback` |
| Token | `POST /connect/token` |
| Pushed authorization (RFC 9126) | `POST /connect/par` |
| Device authorization (RFC 8628) | `POST /connect/deviceauthorization` |
| Introspection (RFC 7662) | `POST /connect/introspect` |
| Revocation (RFC 7009) | `POST /connect/revocation` |
| UserInfo | `GET` and `POST /connect/userinfo` (one record, the same processing for both) |
| End session | `GET` and `POST /connect/endsession` (one record each), `GET /connect/endsession/callback` |
| Check session | `GET /connect/checksession` |
| Nonce | `POST /connect/nonce` |
| Discovery | `GET /.well-known/openid-configuration`, `GET /.well-known/openid-configuration/jwks` |

Routing answers any other method with 405 and an `Allow` header. The endpoints keep checking the content type: a
POST without an `application/x-www-form-urlencoded` body gets 415 or the protocol's `invalid_request` error. They
read the form themselves instead of binding it with `[FromForm]`, which would turn on antiforgery validation.

`AsOidcServerEndpoint` adds what every protocol endpoint shares, as metadata and an endpoint filter:

- **Disabled endpoints.** The endpoint answers 404 when `EndpointsOptions` disables it, and on an mTLS alias
  unless mutual TLS uses the path mode.
- **Unhandled exceptions.** An exception thrown while processing the request or executing its result raises an
  `UnhandledExceptionEvent`.
- **Metadata.** `OidcServerEndpointMetadata` holds the name, the record type and whether the route is an mTLS
  alias.
- **API description.** The endpoint is excluded from the API description, so Swagger documents do not list it.

### Custom endpoints

A custom endpoint is a MinApiLib endpoint in the host's own assembly, mapped with `app.MapEndpoints()`. Call
`AsOidcServerEndpoint` from its `Configure` to give it the same events and logging as the protocol endpoints.

## Services

The default implementation of every service is marked with `[RegisterInIServiceCollection]`. For example:

```csharp
[RegisterInIServiceCollection(typeof(ITokenService))]
public class DefaultTokenService : ITokenService
```

`AddOidcServer()` collects them with MinApiLib's `AddAssembly` and merges them with try-add semantics:

- a service registered **before** `AddOidcServer()` keeps the host's implementation;
- a service registered **after** it (as the builder's `Add*` methods do) is the one the container resolves;
- the services with several implementations (`ISecretParser`, `ISecretValidator` and options setups) are
  added next to the host's.

Only what an attribute cannot express is registered by hand: options, cookie authentication, decorators, HTTP
clients, the back-channel logout background service and framework services. See
[Core.cs](Core/Configuration/Core.cs).

## Differences from the IdentityServer fork

The protocol behaviour is that of the fork on the `archived/identity-server` branch, whose integration test suite
runs against this package.
The one change is that a request with a method the endpoint does not support gets a routing 405. Before, the
endpoint itself answered, with a 405 or, at the token, device authorization and nonce endpoints, a 400
`invalid_request`. The API is renamed:

| IdentityServer | MinApiLib.Oidc.Server |
|---|---|
| `IdentityServer.*` namespaces, by layer (`Models`, `Validation`, `Services`, ...) | `MinApiLib.Oidc.Server.Features.*` and `MinApiLib.Oidc.Server.Core.*`, by feature (see [Layout](#layout)) |
| `IdentityServer.IdentityModel` package | `MinApiLib.Oidc.Server.Core.IdentityModel` namespace, in this package |
| `AddIdentityServer()`, `UseIdentityServer()`, `MapIdentityServer()` | `AddOidcServer()`, `UseOidcServer()`, `MapOidcServer()` |
| `IdentityServerOptions`, `IdentityServerConstants`, `IIdentityServerBuilder` | `OidcServerOptions`, `OidcServerConstants`, `IOidcServerBuilder` |
| `IIdentityServerInteractionService`, `IdentityServerUser`, `IdentityServerTools` | `IOidcServerInteractionService`, `OidcServerUser`, `OidcServerTools` |
| `IEndpointHandler`, `AddEndpoint<T>()` | MinApiLib endpoint records (`GetHandlerAsync`, `PostHandlerAsync`) with `AsOidcServerEndpoint` |
| `AddCoreServices()`, `AddPluggableServices()`, `AddValidators()`, ... | `AddDefaultServices()` (attribute scan) |
| `IdentityServerEndpointMetadata.HandlerType` | `OidcServerEndpointMetadata.EndpointType` |

The protocol values are unchanged. These include the cookie names (`idsrv`, `idsrv.session`,
`idsrv.external`), the routes, the token formats and the discovery document. The values that carried the product
name are renamed:

| IdentityServer | MinApiLib.Oidc.Server |
|---|---|
| Local API scope `IdentityServerApi` | `OidcServerApi` (`OidcServerConstants.LocalApi.ScopeName`) |
| Local API scheme `IdentityServerAccessToken` | `OidcServerAccessToken` |
| `realm="IdentityServer"` in the userinfo endpoint's `WWW-Authenticate` header | `realm="OidcServer"` |
| CORS policy name `IdentityServer` | `OidcServer` (`CorsOptions.CorsPolicyName`) |
| Authentication type of `IdentityServerUser` principals | `OidcServer` |

One addition is off by default. With `options.Authentication.EnforceAcrValues`, the authorize endpoint requires the
session's `acr` claim to be one of the request's `acr_values`; the `idp:` and `tenant:` values are not checked.
Otherwise it sends the user to the login page once, which reads the values from the authorization context and can
ask for a stronger sign-in (step-up). If the session still does not meet them on the way back, the client gets
`unmet_authentication_requirements` at its redirect URI. Without it, as in IdentityServer, `acr_values` only reach
the login page, and a user who already has a session gets tokens whatever the values ask for. Turn it on when the
login page sets the `acr` claim of the sessions it creates.

The fork's `IdentityServer.AspNetIdentity` integration and its unit test suite are not ported. The test users
from `IdentityServer.Test` live in the integration test project
(`tests/MinApiLib.Oidc.Server.IntegrationTests/TestUsers`).

## License

Apache License 2.0. This package is derived from IdentityServer4, Copyright (c) Brock Allen and Dominick Baier,
and from IdentityModel 7.0.0. The repository's [NOTICE](../../NOTICE) holds the required attribution. MinApiLib is
MIT licensed.
