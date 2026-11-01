[[go back to index]](../index.md#documentation-structure)

<a id="refquickstartoverview"></a>

# Overview

The quickstarts build, step by step and from empty projects, the applications of the [samples](../../samples/README.md):
an OpenID Connect server, an API, a worker and a web application. They start with the basics and build on each
other, so do them in order.

1. [Protecting an API using Client Credentials](1_client_credentials.md): a server with its configuration in memory,
   an API that validates its access tokens, and a worker that calls the API with its own token. Then the same with
   DPoP-bound tokens.
2. [Interactive Applications with ASP.NET Core](2_interactive_aspnetcore.md): a login page on the server, and a web
   application that signs users in with OpenID Connect (authorization code flow with PKCE).
3. [ASP.NET Core and API access](3_aspnetcore_and_apis.md): the web application calls the API with the user's access
   token, refreshes it, and binds it to a key with DPoP.
4. [Adding a JavaScript client](4_javascript_client.md)
5. [Using MongoDB for configuration and operational data](5_mongodb.md)
6. [Using ASP.NET Core Identity and passkeys](6_aspnet_identity.md)

The finished code of quickstarts 1 to 3 is in [samples/](../../samples/README.md): `Server`, `Api`, `Worker` and
`WebClient`. The samples do a little more (two clients each, bearer and DPoP side by side, passkeys, MongoDB) and show
every HTTP request they send.

## Prerequisites

* **The .NET 10 SDK** or later. The quickstart projects target `net10.0`; with a later SDK, add
  `--framework net10.0` to `dotnet new`, or target the newer framework, which the packages also support.
* **A trusted development certificate.** The applications talk to each other over HTTPS. Trust the ASP.NET Core
  development certificate once:

  ```bash
  dotnet dev-certs https --trust
  ```

  On Linux the command ends with "There was an error trusting the HTTPS developer certificate", but it trusts the
  certificate for .NET, which the applications use to call each other, and for the browsers. OpenSSL tools such as
  `curl` still do not trust it; for them, set `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs"`.
* **The MinApiLib.Oidc packages.** They are not published to NuGet yet. Build them from a clone of the repository
  into a local folder (this needs the SDK pinned in the repository's `global.json`):

  ```bash
  dotnet pack MinApiLib.Oidc.slnx --configuration Release --output artifacts
  ```

  See [Packages and Builds](../intro/packaging.md).

## Preparation

Create a folder for the quickstart solution, with a `nuget.config` that adds the folder of the packages:

```bash
mkdir quickstart
cd quickstart
dotnet new sln -n Quickstart
dotnet new nugetconfig
dotnet nuget add source /path/to/MinApiLib.Oidc/artifacts --name minapilib-oidc --configfile nuget.config
```

The packages have prerelease versions, so the quickstarts add them with `dotnet add package --prerelease`.

The applications use these addresses, the same as the samples:

| Application | Address |
|---|---|
| Server | https://localhost:5001 |
| WebClient | https://localhost:5002 |
| Api | https://localhost:5003 |
| Worker | (a console application) |

`dotnet new web` gives each project random ports and an `http` profile first. Replace each project's
`Properties/launchSettings.json` with a single `https` profile on its address, for example for the server:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

[[go back to index]](../index.md#documentation-structure)
