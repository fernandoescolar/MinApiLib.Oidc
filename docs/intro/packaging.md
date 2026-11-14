[[go back to index]](../index.md#documentation-structure)

# Packaging and Builds

MinApiLib.Oidc consists of six NuGet packages. They target `net10.0` and `net11.0`. No version has been published
to NuGet yet: build the packages from source (see [Building from source](#building-from-source)).

## Packages

| Package | What it is for | Depends on |
|---|---|---|
| `MinApiLib.Oidc.Server` | The OpenID Connect and OAuth 2.0 server. Its endpoints are MinApiLib endpoint records and its services are registered with MinApiLib's dependency injection attribute. See [its README](../../src/MinApiLib.Oidc.Server/README.md) | `MinApiLib.Oidc.DPoP` |
| `MinApiLib.Oidc.Server.MongoDB` | MongoDB stores for the server: clients, resources, grants, device codes and signing keys. See [its README](../../src/MinApiLib.Oidc.Server.MongoDB/README.md) | `MinApiLib.Oidc.Server` |
| `MinApiLib.Oidc.DPoP` | DPoP (RFC 9449) keys, proofs, thumbprints, nonces, names and proof validation, shared by the clients, the server and the APIs. See [its README](../../src/MinApiLib.Oidc.DPoP/README.md) | |
| `MinApiLib.Oidc.Http` | Access tokens for `HttpClient`, without ASP.NET Core: client credentials tokens, as bearer or DPoP-bound tokens, with caching, renewal and DPoP nonces. See [its README](../../src/MinApiLib.Oidc.Http/README.md) | `MinApiLib.Oidc.DPoP` |
| `MinApiLib.Oidc.Authentication` | The signed-in user's OpenID Connect tokens for `HttpClient` in ASP.NET Core, as bearer or DPoP-bound tokens, refreshed and kept in the session. See [its README](../../src/MinApiLib.Oidc.Authentication/README.md) | `MinApiLib.Oidc.Http` |
| `MinApiLib.Oidc.JwtBearer` | DPoP for ASP.NET Core JWT bearer authentication: `AddJwtBearer().AddDPoP()` validates the proofs of DPoP-bound tokens, rejects them as bearer tokens, issues nonces and announces both schemes. See [its README](../../src/MinApiLib.Oidc.JwtBearer/README.md) | `MinApiLib.Oidc.DPoP` |

So the chains are `Server.MongoDB` → `Server` → `DPoP`, `Authentication` → `Http` → `DPoP`, and `JwtBearer` →
`DPoP`.

Which ones you need:

* **A server**: `MinApiLib.Oidc.Server`, and `MinApiLib.Oidc.Server.MongoDB` to keep its configuration and grants in
  MongoDB.
* **An API**: Microsoft's `AddJwtBearer` is enough for bearer tokens. Add `MinApiLib.Oidc.JwtBearer` for DPoP.
* **A worker, console or service with its own token** (client credentials): `MinApiLib.Oidc.Http`.
* **An ASP.NET Core web application that signs users in** and calls APIs with their tokens:
  `MinApiLib.Oidc.Authentication`, with the ASP.NET Core OpenID Connect handler, which it brings.
* **Requests you build yourself** (proofs, keys, thumbprints): `MinApiLib.Oidc.DPoP`.

The client packages and `MinApiLib.Oidc.JwtBearer` work with any OpenID Connect server, not only
`MinApiLib.Oidc.Server`.

Outside these packages, the server depends on ASP.NET Core, `Microsoft.IdentityModel.*`, `MinApiLib.Endpoints` and
`MinApiLib.DependencyInjection`. It has no dependency on any IdentityServer or IdentityModel package: the protocol
constants it needs live in the `MinApiLib.Oidc.Server.Core.IdentityModel` namespace.

There are no Entity Framework packages, no ASP.NET Core Identity integration package, no quickstart UI and no
`dotnet new` templates. The [samples](../../samples/README.md) show a server with its own login pages.

## Building from source

Requirements: the .NET SDK pinned in `global.json` (11.0.100 or a later feature band).

Clone the repository and build the solution:

```bash
dotnet build MinApiLib.Oidc.slnx
```

Pack the six packages into a local folder:

```bash
dotnet pack MinApiLib.Oidc.slnx --configuration Release --output artifacts
```

To use them from another solution, add that folder as a package source, for example with a `nuget.config` next to
your solution:

```bash
dotnet new nugetconfig
dotnet nuget add source /path/to/MinApiLib.Oidc/artifacts --name minapilib-oidc --configfile nuget.config
```

Versions come from git tags ([MinVer](https://github.com/adamralph/minver)): a release is a tag such as `1.2.0`,
without a prefix. Any other commit builds an `-alpha.0.<height>` prerelease version, so add `--prerelease` to
`dotnet add package`.

The packages ship portable PDBs as `.snupkg` symbol packages with Source Link, XML documentation and a README.
The repository's [README](../../README.md#building) has the details of the build settings, and
[Contributing](contributing.md) how to run the tests.

[[go back to index]](../index.md#documentation-structure)
