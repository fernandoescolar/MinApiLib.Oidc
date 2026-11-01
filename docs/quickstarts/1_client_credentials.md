[[go back to index]](../index.md#documentation-structure)

<a id="refclientcredentialsquickstart"></a>

# Protecting an API using Client Credentials

This first quickstart is the most basic scenario: a server, an API, and a client without a user that calls the API.
The client requests an access token from the server with its client id and secret (the client credentials grant),
and sends it to the API. The API checks that the token comes from the server and was issued for it.

You build three projects:

* **Server**, with `MinApiLib.Oidc.Server` and its configuration in memory;
* **Api**, an orders API protected with ASP.NET Core's `AddJwtBearer`;
* **Worker**, a console application that gets its token with `MinApiLib.Oidc.Http`.

At the end, the same three projects use DPoP-bound tokens instead of bearer tokens.

Do the [preparation](0_overview.md#preparation) first. The finished code is in
[samples/](../../samples/README.md): `Server`, `Api` and `Worker`.

## Creating the server

From the solution folder:

```bash
dotnet new web -n Server
dotnet sln add Server
dotnet add Server package MinApiLib.Oidc.Server --prerelease
```

Set the server's address to `https://localhost:5001` in `Server/Properties/launchSettings.json`, as the
[overview](0_overview.md#preparation) shows.

## Defining an API

An API is a resource you want to protect. The server describes it with two kinds of objects:

* an **API scope** is what a client can ask for, here `orders.read`;
* an **API resource** is the API itself. Its name becomes the audience (`aud`) of the access token, which the API
  checks, and it lists the scopes that belong to it.

The quickstarts define them in code. Add a `Config.cs` file to the server:

```csharp
using MinApiLib.Oidc.Server.Core.ClientAuthentication;
using MinApiLib.Oidc.Server.Core.Models;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("orders.read", "Read your orders")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("orders", "Orders API") { Scopes = { "orders.read" } }
    ];
}
```

Give your APIs and scopes names that describe them to the developers who use them, and to the users who see them on
a consent page. See [Defining Resources](../topics/resources.md).

## Defining the client

Next, define the client that calls the API. It has no user, and it authenticates with a secret. Add this to
`Config`:

```csharp
public static IEnumerable<Client> Clients =>
[
    new Client
    {
        ClientId = "worker",

        // no user: the client authenticates with its own id and secret
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        ClientSecrets = { new Secret("worker-secret".Sha256()) },

        // the scopes the client can ask for
        AllowedScopes = { "orders.read" }
    }
];
```

Think of the client id and secret as the login and password of the application itself. The server stores the hash of
the secret only (`Sha256()`, from `MinApiLib.Oidc.Server.Core.ClientAuthentication`).

## Configuring the server

Replace `Server/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOidcServer()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddDeveloperSigningCredential();   // development only: a key in tempkey.jwk

var app = builder.Build();

app.UseRouting();
app.UseOidcServer();   // session cookie, CORS, authentication, mutual TLS, logout notifications
app.MapOidcServer();   // the protocol endpoints

app.Run();
```

`AddOidcServer()` registers the server and returns an `IOidcServerBuilder`, on which you add the configuration and
the signing key. `UseOidcServer()` adds its middleware and `MapOidcServer()` maps the protocol endpoints; both are
required.

`AddDeveloperSigningCredential()` creates an RSA key at the first start and keeps it in `tempkey.jwk`, in the
current directory. Don't commit that file: the server creates it again when it is missing. In production, use a real
key with `AddSigningCredential(...)`, or let the server create and rotate its keys with
`options.KeyManagement.Enabled = true`, as the sample server does. See
[Cryptography, Keys and HTTPS](../topics/crypto.md).

Run the server:

```bash
dotnet run --project Server
```

Open `https://localhost:5001/.well-known/openid-configuration`. This is the discovery document: clients and APIs read
the server's endpoints and keys from it, so they only need to know the server's address.

```json
{
  "issuer": "https://localhost:5001",
  "jwks_uri": "https://localhost:5001/.well-known/openid-configuration/jwks",
  "authorization_endpoint": "https://localhost:5001/connect/authorize",
  "token_endpoint": "https://localhost:5001/connect/token",
  ...
}
```

The server can already issue tokens. A token request with the client's credentials:

```http
POST /connect/token HTTP/1.1
Host: localhost:5001
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=worker&client_secret=worker-secret&scope=orders.read
```

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IkMyMzQ5NDI3...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "scope": "orders.read"
}
```

The access token is a JWT of type `at+jwt`. Decoded, its payload has the issuer, the audience (the API resource),
the client, the scopes and the lifetime:

```json
{
  "iss": "https://localhost:5001",
  "aud": "orders",
  "client_id": "worker",
  "scope": [ "orders.read" ],
  "jti": "72D62815487F13A56E26D349F0050C68",
  "iat": 1794912817,
  "nbf": 1794912817,
  "exp": 1794916417
}
```

## Adding an API

Create the API, with `MinApiLib.Oidc.JwtBearer`. You only need it for DPoP, at the end of this quickstart, but it
also brings Microsoft's `Microsoft.AspNetCore.Authentication.JwtBearer` in the version that matches your framework:

```bash
dotnet new web -n Api
dotnet sln add Api
dotnet add Api package MinApiLib.Oidc.JwtBearer --prerelease
```

Set its address to `https://localhost:5003` in `Api/Properties/launchSettings.json`, and replace `Api/Program.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // the server: the handler reads its discovery document and signing keys
        options.Authority = "https://localhost:5001";

        // the API resource: tokens for other APIs are rejected
        options.Audience = "orders";

        // keep the claim names of the token ("sub", "scope", "client_id")
        options.MapInboundClaims = false;

        // access tokens only, not identity tokens or other JWTs of the server
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "orders.read"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/orders", (ClaimsPrincipal user) => new
{
    client = user.FindFirst("client_id")?.Value,
    user = user.FindFirst("sub")?.Value,
    orders = new[]
    {
        new { id = 1001, total = 42.50m, status = "shipped" },
        new { id = 1002, total = 18.00m, status = "processing" }
    }
}).RequireAuthorization("orders.read");

app.Run();
```

`AddJwtBearer` validates each token: its signature with the server's keys, the issuer, the audience, the type and the
lifetime. The `orders.read` policy then requires the scope. A token for the same API without that scope gets `403`.

Run the API next to the server and open `https://localhost:5003/orders`. Without a token, the API answers `401`.

## Creating the client

The last project is the worker. Create a console application with `MinApiLib.Oidc.Http` and the .NET generic host:

```bash
dotnet new console -n Worker
dotnet sln add Worker
dotnet add Worker package MinApiLib.Oidc.Http --prerelease
dotnet add Worker package Microsoft.Extensions.Hosting
```

You could request the token with a plain HTTP request, as above. `MinApiLib.Oidc.Http` does it for you: it finds the
token endpoint in the discovery document, requests a token on the first call, keeps it until a minute before it
expires, and adds it to every request of an `HttpClient`.

Replace `Worker/Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// the client: how to get its token
builder.Services.AddClientCredentialsTokens("orders", options =>
{
    options.Authority = "https://localhost:5001";
    options.ClientId = "worker";
    options.ClientSecret = "worker-secret";
    options.Scope = "orders.read";
});

// the API: each request carries the client's token
builder.Services.AddHttpClient("orders", client => client.BaseAddress = new Uri("https://localhost:5003"))
    .AddClientCredentialsTokenHandler("orders");

using var host = builder.Build();

var orders = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient("orders");
using var response = await orders.GetAsync("/orders");

Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}");
Console.WriteLine(await response.Content.ReadAsStringAsync());
```

`AddClientCredentialsTokens` registers a client under a name, and `AddClientCredentialsTokenHandler` sends that
client's token with every request of the HTTP client. The worker never sees the token. To get it yourself, use
`IClientCredentialsTokenManager` (namespace `MinApiLib.Oidc.Http.ClientCredentials`):
`await tokens.GetAccessTokenAsync("orders")`.

## Calling the API

With the server and the API running, run the worker:

```bash
dotnet run --project Worker
```

The request it sends, and the answer:

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6...
```

```text
200 OK
{"client":"worker","user":null,"orders":[{"id":1001,"total":42.50,"status":"shipped"},{"id":1002,"total":18.00,"status":"processing"}]}
```

There is no `sub`: the token belongs to the client, not to a user.

## Binding the token to a key with DPoP

A bearer token works for whoever holds it. A DPoP-bound token ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449))
only works with a proof signed by a key that only the client holds. Three changes turn it on.

In the worker, create a key and bind the token to it:

```csharp
builder.Services.AddClientCredentialsTokens("orders", options =>
{
    // ...
    options.UseDPoP = true;
});
```

The worker creates an EC P-256 key in memory and sends a proof of it with the token request. The server binds the
token to the key: it answers `"token_type": "DPoP"` and the token carries the key's thumbprint in `cnf.jkt`. Each
API call then carries the token with the `DPoP` scheme and a new proof for that method and URL:

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6...
DPoP: eyJhbGciOiJFUzI1NiIsImp3ayI6eyJrdHkiOiJFQyIs...
```

In the API, add one line after `AddJwtBearer` (`MinApiLib.Oidc.JwtBearer`):

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* as before */ })
    .AddDPoP();
```

`AddDPoP()` accepts `Authorization: DPoP`, checks the proof (signature, method, URL, age, token hash, replay) and that
its key is the one in `cnf.jkt`, and rejects a DPoP-bound token sent as a bearer token. Bearer tokens still work;
`options.Mode = DPoPTokenMode.Required` accepts DPoP-bound tokens only. In an endpoint,
`HttpContext.GetAccessTokenScheme()` and `HttpContext.GetDPoPThumbprint()` (namespace `MinApiLib.Oidc.JwtBearer`)
tell how the request was authenticated.

On the server, nothing is needed: a token request with a proof gets a DPoP-bound token. To refuse token requests
without a proof, set `RequireDPoP` on the client:

```csharp
new Client
{
    ClientId = "worker",
    // ...
    RequireDPoP = true
}
```

[Proof-of-Possession Access Tokens](../topics/pop.md) walks through the exchange message by message. The sample worker
runs both variants, with the clients `worker` and `worker.dpop`, and prints each proof.

## Further experiments

So far the walkthrough followed the success path. Try to provoke errors to learn how the system behaves:

* stop the server and run the worker: the token request fails;
* use a wrong client id or secret: the server answers `invalid_client`, and the worker throws an
  `AccessTokenException` with the error;
* ask for a scope the client is not allowed: `invalid_scope`;
* call the API without a token, or with a token whose `aud` is not `orders`: `401`;
* require a scope in the API that is not in the token: `403`;
* with DPoP, send the token as `Authorization: Bearer`: the API answers `401` with `invalid_token`.

[[go back to index]](../index.md#documentation-structure)
