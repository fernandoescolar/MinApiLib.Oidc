[[go back to index]](../index.md#documentation-structure)

<a id="refprotectingapis"></a>

# Protecting APIs

The server issues access tokens in the [JWT](https://tools.ietf.org/html/rfc7519) format (JSON Web Token, with
`typ: at+jwt`) by default. Every relevant platform can validate JWTs; [jwt.io](https://jwt.io) lists libraries.

## ASP.NET Core APIs

An ASP.NET Core API validates the tokens with Microsoft's JWT bearer authentication handler
(`Microsoft.AspNetCore.Authentication.JwtBearer`). It reads the server's discovery document and signing keys from the
`Authority`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // the issuer URI of the server
        options.Authority = "https://login.example.com";

        // the name of the API resource
        options.Audience = "orders";

        // keep the claim names of the token ("sub", "scope", ...)
        options.MapInboundClaims = false;

        // the server emits a typ header; checking it is recommended
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("orders.read", policy => policy.RequireClaim("scope", "orders.read"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/orders", () => Results.Ok(Array.Empty<object>()))
   .RequireAuthorization("orders.read");

app.Run();
```

The audience is the name of an API resource. Tokens for API scopes that belong to no API resource carry the static
audience `{issuer}/resources`. See [here](resources.md#refapiresources) for more information on resources, scopes,
audiences and authorization.

### Authorization based on scopes

A token carries its granted scopes in the `scope` claim: one claim per scope in ASP.NET Core, since the server emits
them as a JSON array (unless `EmitScopesAsSpaceDelimitedStringInJwt` is set). Check them with authorization policies,
as above: `RequireClaim("scope", "orders.read")` requires that scope. The authentication handler does not check
scopes by itself.

## DPoP-bound tokens

A client with `RequireDPoP`, or one that sends a `DPoP` proof to the token endpoint, gets an access token bound to its
key (`cnf.jkt`). It calls the API with `Authorization: DPoP <token>` and a new proof on each request. Microsoft's JWT
bearer handler does not check proofs: add `MinApiLib.Oidc.JwtBearer` and call `.AddDPoP()` after `AddJwtBearer`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.example.com";
        options.Audience = "orders";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    })
    .AddDPoP();
```

The API then accepts DPoP-bound tokens with a valid proof of the bound key, rejects them when they are sent as bearer
tokens, and keeps accepting bearer tokens unless `options.Mode = DPoPTokenMode.Required`. In an endpoint,
`HttpContext.GetAccessTokenScheme()` says which scheme the request used and `HttpContext.GetDPoPThumbprint()` gives
the key. See [its README](../../src/MinApiLib.Oidc.JwtBearer/README.md) for the options (nonces, replay cache) and
[Proof-of-Possession Access Tokens](pop.md) for the exchange, message by message. The
[sample API](../../samples/Api/Program.cs) is configured this way.

## Validating reference tokens

[Reference tokens](reference_tokens.md) are opaque handles: the API has to ask the server about each one at the
[introspection endpoint](../endpoints/introspection.md) (RFC 7662), authenticated as an API resource with a secret:

```http
POST /connect/introspect HTTP/1.1
Host: login.example.com
Authorization: Basic b3JkZXJzOnNlY3JldA==
Content-Type: application/x-www-form-urlencoded

token=8BA1D2E6F3B9C47A
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "iss": "https://login.example.com",
  "client_id": "worker",
  "aud": "orders",
  "active": true,
  "scope": "orders.read"
}
```

Microsoft does not ship an introspection authentication handler for ASP.NET Core, and MinApiLib.Oidc does not
include one either. To accept reference tokens, use a third-party OAuth 2.0 introspection handler, or write one
over the request above. Otherwise, use JWT access tokens, which `AddJwtBearer` validates without calling the server.

## Local APIs

APIs hosted in the server's own application can validate the server's access tokens in process, JWT or reference,
with the local API authentication handler of `MinApiLib.Oidc.Server` (namespace
`MinApiLib.Oidc.Server.Features.LocalApi`):

```csharp
builder.Services.AddLocalApiAuthentication();
```

`AddLocalApiAuthentication` registers the `OidcServerAccessToken` scheme (`OidcServerConstants.LocalApi.AuthenticationScheme`),
which requires the `OidcServerApi` scope (`OidcServerConstants.LocalApi.ScopeName`), and an authorization policy of the
same name (`OidcServerConstants.LocalApi.PolicyName`):

```csharp
app.MapGet("/localApi", () => Results.Ok())
   .RequireAuthorization(OidcServerConstants.LocalApi.PolicyName);
```

For another scope or scheme name, register the handler yourself with `AddAuthentication().AddLocalApi(...)` and set
`LocalApiAuthenticationOptions.ExpectedScope`. The handler accepts DPoP-bound tokens with their proofs. See
[Adding more API Endpoints](add_apis.md).

[[go back to index]](../index.md#documentation-structure)
