# IdentityServer

A maintained fork of [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 4.1.x,
updated to current .NET (`net10.0` and `net11.0`) and maintained by @fernandoescolar.

IdentityServer4 reached end of life in November 2022 and its successor is commercial. This fork
keeps the original Apache-2.0 code base alive: same protocol implementation and extensibility
points, modernised build, dependencies and runtime. It is not affiliated with, nor endorsed by,
the original authors or Duende Software.

A full assessment of the code base and the roadmap for the fork is in
[docs/analysis/2026-11-analisis-producto-y-mejoras.md](docs/analysis/2026-11-analisis-producto-y-mejoras.md).

## Packages

| Package | Contents |
|---|---|
| `IdentityServer` | OpenID Connect / OAuth 2.0 engine for ASP.NET Core |
| `IdentityServer.Models` | Clients, resources, grants and protocol constants |
| `IdentityServer.IdentityModel` | OpenID Connect / OAuth 2.0 constants and helpers (`OidcConstants`, `JwtClaimTypes`, `CryptoRandom`), derived from IdentityModel 7.0.0; replaces the external `IdentityModel` package |
| `IdentityServer.Storage` | Store abstractions and in-memory stores |
| `IdentityServer.AspNetIdentity` | ASP.NET Core Identity integration |
| `MinApiLib.Oidc.Server` | A new implementation derived from this fork, in a single package: the endpoints are [MinApiLib](https://github.com/fernandoescolar/MinApiLib) endpoint records and the services are registered with MinApiLib's dependency injection attribute. It does not depend on any `IdentityServer` package. See [its README](src/MinApiLib.Oidc.Server/README.md) |
| `MinApiLib.Oidc.Server.MongoDB` | MongoDB stores for `MinApiLib.Oidc.Server`: clients, resources, grants, device codes and signing keys. See [its README](src/MinApiLib.Oidc.Server.MongoDB/README.md) |
| `MinApiLib.Oidc.DPoP` | DPoP (RFC 9449) keys, proofs, thumbprints, nonces and names, shared by the clients and the server. See [its README](src/MinApiLib.Oidc.DPoP/README.md) |
| `MinApiLib.Oidc.Http` | Access tokens for `HttpClient`, without ASP.NET Core: client credentials tokens, as bearer or DPoP-bound tokens, with caching, renewal and DPoP nonces. See [its README](src/MinApiLib.Oidc.Http/README.md) |
| `MinApiLib.Oidc.Authentication` | The signed-in user's OpenID Connect tokens for `HttpClient` in ASP.NET Core, as bearer or DPoP-bound tokens, refreshed and kept in the session. See [its README](src/MinApiLib.Oidc.Authentication/README.md) |

## Hosting

The protocol endpoints are served through ASP.NET Core endpoint routing. `UseIdentityServer()` adds the
IdentityServer middleware (session cookie, CORS, authentication, mutual TLS and logout notifications) and
`MapIdentityServer()` maps the endpoints. The conventions builder applies per-endpoint policies such as rate
limiting or output caching:

```csharp
var app = builder.Build();

app.UseIdentityServer();
app.UseRateLimiter();

var identityServer = app.MapIdentityServer();
identityServer.ForEndpoint(IdentityServerConstants.EndpointNames.Token).RequireRateLimiting("token");
identityServer.ForEndpoint(IdentityServerConstants.EndpointNames.Discovery).CacheOutput();
```

Endpoints added with `AddEndpoint<T>` are mapped as well, together with their `/connect/mtls/...` aliases when
mutual TLS is enabled. A disabled endpoint answers 404.

Back-channel logout notifications are sent by a background service by default, so slow clients do not delay the
user's sign-out. Set `IdentityServerOptions.Authentication.SendBackChannelLogoutNotificationsInBackground` to
`false` if a custom `IBackChannelLogoutService` needs the original HTTP request.

## Protocol features beyond IdentityServer4

| Feature | Configuration | Documentation |
|---|---|---|
| Pushed authorization requests (RFC 9126) | `/connect/par`, on by default; `Client.RequirePushedAuthorization` | [docs/topics/par.md](docs/topics/par.md) |
| DPoP (RFC 9449) | Used when the client sends a `DPoP` header; `Client.RequireDPoP` | [docs/topics/pop.md](docs/topics/pop.md#dpop) |
| Resource indicators (RFC 8707) | `resource` parameter; `ApiResource.RequireResourceIndicator` | [docs/topics/resources.md](docs/topics/resources.md#resource-indicators) |
| Automatic signing key rotation | `IdentityServerOptions.KeyManagement.Enabled`, off by default | [docs/topics/crypto.md](docs/topics/crypto.md#automatic-key-management) |
| Issuer in authorization responses (RFC 9207) | `EmitIssuerIdentificationResponseParameter`, on by default | — |
| Custom `prompt` values (for example `create` or `register`) | `AddPromptRedirect(prompt, url)` or `AddPromptHandler<T>(prompt)`; `UserInteraction.PromptValuesSupported` | [docs/topics/prompt.md](docs/topics/prompt.md) |
| `client_id` at the end session endpoint | Validates `post_logout_redirect_uri` without an `id_token_hint`; the sign-out prompt is still shown | [docs/topics/signout.md](docs/topics/signout.md) |

## Deprecated flows

The implicit flow, access tokens issued by the authorize endpoint and the resource owner password grant are
deprecated by OAuth 2.1 and RFC 9700. They are available for existing clients, off by default, behind
`Client.AllowImplicitFlow`, `Client.AllowAccessTokensViaBrowser` and `Client.AllowResourceOwnerPasswordFlow`. See
[docs/topics/grant_types.md](docs/topics/grant_types.md#deprecated-flows).

## Building

Requirements: .NET SDK 11.0.100 or a later feature band (pinned in `global.json`).

```bash
dotnet build IdentityServer.slnx
```

```bash
dotnet test IdentityServer.slnx
```

```bash
dotnet pack IdentityServer.slnx --configuration Release --output artifacts
```

Build settings shared by every project live in `Directory.Build.props`; package versions are managed
centrally in `Directory.Packages.props`. Warnings are errors, code style is enforced in the build and
the .NET analyzers run in `Recommended` mode. Nullable reference types are enabled in every library.
Library code logs only through source-generated `[LoggerMessage]` methods (CA1848 is an error). The few
analyzer rules still lowered in `.editorconfig` would require breaking public API changes; `tests/.editorconfig`
relaxes some rules for test code. Tests use the xUnit assertions only; the integration tests carry a small
test-only protocol client in `Common/ProtocolClient`, ported from IdentityModel 7.0.0.

### Snapshot tests

`tests/IdentityServer.IntegrationTests/Snapshots` pins the discovery document and the shape of an access
token in `*.verified.json` files. A change writes a `*.received.json` file next to it; if the change is
intended, replace the verified file with it.

### Mutation testing

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) is installed as a local tool and
configured in `stryker-config.json`: it mutates the validators, endpoints, response generators and default
services of the core project and runs the test projects of the solution against each mutant. A full run takes
a long time; narrow it with `--mutate`:

```bash
dotnet tool restore
```

```bash
dotnet stryker --mutate "**/Validation/Default/DefaultDPoPProofValidator.cs"
```

The HTML report is written to `StrykerOutput/`.

### Versioning

Versions come from git tags via [MinVer](https://github.com/adamralph/minver): tag a commit `vX.Y.Z` to
release that version; any other commit builds as `X.Y.(Z+1)-preview.0.<height>`. CI clones with full
history and tags for this reason.

### Source Link and symbols

Packages ship portable PDBs as `.snupkg` with Source Link enabled. The repository has no remote yet,
so `Microsoft.SourceLink.GitHub` is referenced provisionally; switch to `Microsoft.SourceLink.AzureRepos.Git`
if the code is hosted on Azure Repos, and fill in `RepositoryUrl` in `Directory.Build.props`.

### Continuous integration

`deploy/pipelines/main.yml` is an Azure DevOps pipeline: restore, build with `-warnaserror`, test with
coverage, pack, and publish the packages as a build artifact. Tags `v*` additionally push the packages to
an Azure Artifacts feed (the feed name is a placeholder to fill in).

## Samples

[samples/](samples/README.md) has a server built with `MinApiLib.Oidc.Server` (in memory or on MongoDB), a web
client, an API and a worker. The web client signs the user in and calls the API with the user's access token, and
the worker with its own token (client credentials), each as a bearer token or as a DPoP-bound token. They use
`MinApiLib.Oidc.Authentication` and `MinApiLib.Oidc.Http` and show each HTTP request and response.

## Changes from IdentityServer4

[CHANGELOG.md](CHANGELOG.md) summarises what changed from IdentityServer4 4.1.2.

## Documentation

The original IdentityServer4 documentation is kept under [docs/](docs/index.md). It still describes the
upstream project; package names and some defaults differ in this fork.

## License and attribution

Licensed under the [Apache License 2.0](LICENSE). IdentityServer4 is Copyright (c) Brock Allen and
Dominick Baier; see [NOTICE](NOTICE) for the required attribution.

## Acknowledgements

IdentityServer is built using the following great open source projects:

* [ASP.NET Core](https://github.com/dotnet/aspnetcore)
* [MinVer](https://github.com/adamralph/minver)
* [xUnit](https://xunit.net/)
* [IdentityModel](https://github.com/IdentityModel/IdentityModel) (the source of `IdentityServer.IdentityModel`)
* [MinApiLib](https://github.com/fernandoescolar/MinApiLib) (endpoints and dependency injection of `MinApiLib.Oidc.Server`)
