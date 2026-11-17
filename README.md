# MinApiLib.Oidc

OpenID Connect and OAuth 2.0 for ASP.NET Core, server and clients, with proof of possession (DPoP, RFC 9449) from
end to end. Built on [MinApiLib](https://github.com/fernandoescolar/MinApiLib), targeting `net10.0` and `net11.0`,
and maintained by @fernandoescolar.

The server includes code derived from [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 4.1.2
(Apache-2.0); the client and API packages are original to this project (see [License](#license)). MinApiLib.Oidc is
not affiliated with, nor endorsed by, the authors of IdentityServer4 or Duende Software.

## Packages

| Package | Contents |
|---|---|
| `MinApiLib.Oidc.Server` | OpenID Connect and OAuth 2.0 server. The endpoints are MinApiLib endpoint records and the services are registered with MinApiLib's dependency injection attribute. See [its README](src/MinApiLib.Oidc.Server/README.md) |
| `MinApiLib.Oidc.Server.MongoDB` | MongoDB stores for `MinApiLib.Oidc.Server`: clients, resources, grants, device codes and signing keys. See [its README](src/MinApiLib.Oidc.Server.MongoDB/README.md) |
| `MinApiLib.Oidc.DPoP` | DPoP (RFC 9449) keys, proofs, thumbprints, nonces, names and proof validation, shared by the clients, the server and the APIs. See [its README](src/MinApiLib.Oidc.DPoP/README.md) |
| `MinApiLib.Oidc.Http` | Access tokens for `HttpClient`, without ASP.NET Core: client credentials tokens, as bearer or DPoP-bound tokens, with caching, renewal and DPoP nonces. See [its README](src/MinApiLib.Oidc.Http/README.md) |
| `MinApiLib.Oidc.Authentication` | The signed-in user's OpenID Connect tokens for `HttpClient` in ASP.NET Core, as bearer or DPoP-bound tokens, refreshed and kept in the session. See [its README](src/MinApiLib.Oidc.Authentication/README.md) |
| `MinApiLib.Oidc.JwtBearer` | DPoP for ASP.NET Core JWT bearer authentication: `AddJwtBearer().AddDPoP()` validates the proofs of DPoP-bound tokens, rejects them as bearer tokens, issues nonces and announces both schemes. See [its README](src/MinApiLib.Oidc.JwtBearer/README.md) |

They depend on each other like this: `Server.MongoDB` → `Server` → `DPoP`, `Authentication` → `Http` → `DPoP`, and
`JwtBearer` → `DPoP`.

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
app.UseOidcServer();
app.UseAuthorization();
app.MapOidcServer()
   .ForEndpoint(OidcServerConstants.EndpointNames.Token).RequireRateLimiting("token");

app.Run();
```

The [server README](src/MinApiLib.Oidc.Server/README.md) describes its layout, endpoints and services, and how its API
maps to IdentityServer's.

## Protocol features beyond IdentityServer4

| Feature | Configuration |
|---|---|
| Pushed authorization requests (RFC 9126) | `/connect/par`, on by default; `Client.RequirePushedAuthorization`. See [docs/topics/par.md](docs/topics/par.md) |
| DPoP (RFC 9449) | Used when the client sends a `DPoP` header; `Client.RequireDPoP`. See [docs/topics/pop.md](docs/topics/pop.md) |
| Resource indicators (RFC 8707) | `resource` parameter; `ApiResource.RequireResourceIndicator`. See [docs/topics/resources.md](docs/topics/resources.md) |
| Automatic signing key rotation | `OidcServerOptions.KeyManagement.Enabled`, off by default. See [docs/topics/crypto.md](docs/topics/crypto.md) |
| Issuer in authorization responses (RFC 9207) | `EmitIssuerIdentificationResponseParameter`, on by default |
| Custom `prompt` values (for example `create` or `register`) | `AddPromptRedirect(prompt, url)` or `AddPromptHandler<T>(prompt)`; `UserInteraction.PromptValuesSupported`. See [docs/topics/prompt.md](docs/topics/prompt.md) |
| `client_id` at the end session endpoint | Validates `post_logout_redirect_uri` without an `id_token_hint`; the sign-out prompt is still shown. See [docs/endpoints/endsession.md](docs/endpoints/endsession.md) |
| Required `acr_values` and step-up | `Authentication.EnforceAcrValues`, off by default. See [docs/quickstarts/6_aspnet_identity.md](docs/quickstarts/6_aspnet_identity.md) |

The implicit flow, access tokens issued by the authorize endpoint and the resource owner password grant are
deprecated by OAuth 2.1 and RFC 9700. They are available for existing clients, off by default, behind
`Client.AllowImplicitFlow`, `Client.AllowAccessTokensViaBrowser` and `Client.AllowResourceOwnerPasswordFlow`. See
[docs/topics/grant_types.md](docs/topics/grant_types.md).

## Samples

[samples/](samples/README.md) has a server built with `MinApiLib.Oidc.Server` (in memory or on MongoDB), a web
client, an API and a worker. The web client signs the user in and calls the API with the user's access token, and
the worker with its own token (client credentials), each as a bearer token or as a DPoP-bound token. They use
`MinApiLib.Oidc.Authentication`, `MinApiLib.Oidc.Http` and `MinApiLib.Oidc.JwtBearer` and show each HTTP request and
response. The server's login accepts passwords and passkeys (ASP.NET Core Identity). The pages of the server and
the web client are React + TypeScript + Tailwind, built with Vite. `dotnet run apphost.cs` in `samples/` starts them
all, with MongoDB, with Aspire.

## Building

Requirements: .NET SDK 11.0.100 or a later feature band (pinned in `global.json`), and Node.js 20.19 or later for
the samples' frontends, which the build installs and builds with npm. Without Node.js, add `-p:SkipClientApp=true`
to build everything else.

```bash
dotnet build MinApiLib.Oidc.slnx
```

```bash
dotnet test --solution MinApiLib.Oidc.slnx
```

```bash
dotnet pack MinApiLib.Oidc.slnx --configuration Release --output artifacts
```

The MongoDB tests run against MongoDB in Docker (Testcontainers), so they need Docker.

Build settings shared by every project live in `Directory.Build.props`; package versions are managed
centrally in `Directory.Packages.props`. Warnings are errors, code style is enforced in the build and
the .NET analyzers run in `Recommended` mode. Nullable reference types are enabled in every library.
Library code logs only through source-generated `[LoggerMessage]` methods (CA1848 is an error). `tests/.editorconfig`
relaxes some rules for test code. Tests use the xUnit assertions only; the server's integration tests carry a small
test-only protocol client in `Common/ProtocolClient`, ported from IdentityModel 7.0.0.

### Snapshot tests

`tests/MinApiLib.Oidc.Server.IntegrationTests/Snapshots` pins the discovery document and the shape of an access
token in `*.verified.json` files. A change writes a `*.received.json` file next to it; if the change is
intended, replace the verified file with it.

### Mutation testing

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) is installed as a local tool and
configured in `stryker-config.json`: it mutates the features and core services of `MinApiLib.Oidc.Server` and runs
the test projects of the solution against each mutant. A full run takes a long time; narrow it with `--mutate`:

```bash
dotnet tool restore
```

```bash
dotnet stryker --mutate "**/Core/DPoP/DefaultDPoPProofValidator.cs"
```

The HTML report is written to `StrykerOutput/`.

### Versioning

Versions come from git tags via [MinVer](https://github.com/adamralph/minver), with its default settings: tag a
commit `X.Y.Z`, without a prefix, to release that version; any other commit builds as
`X.Y.(Z+1)-alpha.0.<height>`, and `0.0.0-alpha.0.<height>` before the first tag. CI clones with full
history and tags for this reason.

### Releases

Pushing a tag `X.Y.Z` (or `X.Y.Z-suffix`) runs [release.yml](.github/workflows/release.yml): it builds, tests and
packs, publishes the packages to nuget.org with [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
(no API key is stored; the `NUGET_USER` secret holds the nuget.org profile name of the policy's owner), and then
creates the GitHub release with the packages.

```bash
git tag 1.0.0
git push origin 1.0.0
```

### Source Link and symbols

Packages ship portable PDBs as `.snupkg` with Source Link enabled (`Microsoft.SourceLink.GitHub`), and point to
the repository through `RepositoryUrl` in `Directory.Build.props`.

## Coming from IdentityServer4

[CHANGELOG.md](CHANGELOG.md) summarises what is different from IdentityServer4 4.1.2 and how to move a host. The
`archived/identity-server` branch keeps the IdentityServer packages this project grew out of; they receive no further
changes.

## Documentation

[docs/](docs/index.md) has quickstarts, topics, the endpoints and a reference of the server's options and models;
parts of it derive from the IdentityServer4 documentation. Each package also has its README, and
[docs/topics/pop.md](docs/topics/pop.md) walks through proof of possession message by message.

## License

Copyright (c) Fernando Escolar. Licensed under the [Apache License 2.0](LICENSE).

`MinApiLib.Oidc.Server`, its integration tests and parts of the documentation include code and text derived from
IdentityServer4, and the server's `Core.IdentityModel` namespace from IdentityModel. Both are Copyright (c) Brock Allen
and Dominick Baier and licensed under the Apache License 2.0; [NOTICE](NOTICE) carries their attribution. The other
packages, `MinApiLib.Oidc.DPoP`, `.Http`, `.Authentication`, `.JwtBearer` and `.Server.MongoDB`, are original to
MinApiLib.Oidc. MinApiLib is MIT licensed.

## Acknowledgements

MinApiLib.Oidc is built using the following great open source projects:

* [ASP.NET Core](https://github.com/dotnet/aspnetcore)
* [MinApiLib](https://github.com/fernandoescolar/MinApiLib)
* [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) (the origin of parts of `MinApiLib.Oidc.Server`)
* [IdentityModel](https://github.com/IdentityModel/IdentityModel) (the source of `MinApiLib.Oidc.Server.Core.IdentityModel`)
* [MongoDB C# driver](https://github.com/mongodb/mongo-csharp-driver)
* [MinVer](https://github.com/adamralph/minver)
* [xUnit](https://xunit.net/)
