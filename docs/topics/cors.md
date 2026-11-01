[[go back to index]](../index.md#documentation-structure)

# CORS

JavaScript clients running in the browser call several of the server's endpoints. The server is usually hosted on a
different origin than these clients, so [Cross-Origin Resource Sharing](https://developer.mozilla.org/docs/Web/HTTP/CORS)
(CORS) has to be configured.

`UseOidcServer()` applies the server's CORS policy to the endpoints browsers call: discovery and its key set, token,
userinfo, revocation and pushed authorization. `OidcServerOptions.Cors` (`CorsOptions`) holds the policy name
(`CorsPolicyName`), these paths (`CorsPaths`) and the preflight cache duration (`PreflightCacheDuration`).

## Client-based CORS Configuration

One approach to configuring CORS is to use the `AllowedCorsOrigins` collection on the
[client configuration](../reference/client.md). Add the origin of the client to the collection and the server allows
cross-origin calls from that origin.

#### NOTE
Be sure to use an origin (not a URL) when configuring CORS. For example: `https://foo:123/` is a URL, whereas
`https://foo:123` is an origin.

This is what you get with `AddInMemoryClients` and with the MongoDB stores of
[MinApiLib.Oidc.Server.MongoDB](../../src/MinApiLib.Oidc.Server.MongoDB/README.md) (`AddMongoStores`). If you write
your own `IClientStore`, also write a CORS policy service for it (see below).

## Custom Cors Policy Service

The host can implement `ICorsPolicyService` (namespace `MinApiLib.Oidc.Server.Core.Cors`) to control the CORS policy
completely.

The single method to implement is `Task<bool> IsOriginAllowedAsync(string origin)`. Return `true` if the origin is
allowed, `false` otherwise.

Register it with the builder:

```csharp
builder.Services.AddOidcServer()
    .AddCorsPolicyService<MyCorsPolicyService>();
```

`AddCorsPolicyCache<T>()` registers it behind a cache instead, kept for `OidcServerOptions.Caching.CorsExpiration`
(it needs `AddInMemoryCaching()`).

**DefaultCorsPolicyService**

If you want to hard-code a set of allowed origins, use the `DefaultCorsPolicyService` implementation. Register it as a
singleton, after `AddOidcServer()`, with its `AllowedOrigins` collection, or set `AllowAll` to `true` to allow all
origins:

```csharp
builder.Services.AddOidcServer();

builder.Services.AddSingleton<ICorsPolicyService>(services =>
    new DefaultCorsPolicyService(services.GetRequiredService<ILogger<DefaultCorsPolicyService>>())
    {
        AllowedOrigins = { "https://foo", "https://bar" }
    });
```

#### NOTE
Use `AllowAll` with caution.

## Mixing the server's CORS policy with ASP.NET Core's CORS policies

The server uses ASP.NET Core's CORS services and middleware. The application that hosts it might also need CORS for
its own endpoints. Both work together in the same application.

Use the documented CORS features of ASP.NET Core without regard to the server: define policies with
`builder.Services.AddCors(...)` and apply them with `app.UseCors(...)` or `RequireCors(...)` on your endpoints, as
usual.

The one scenario where there might be a conflict is a custom `ICorsPolicyProvider`. The server registers its own
`ICorsPolicyProvider`, which decorates the one already registered: it answers for its own paths and policy name, and
delegates everything else. So you can also implement `ICorsPolicyProvider`, but register it before `AddOidcServer()`.

[[go back to index]](../index.md#documentation-structure)
