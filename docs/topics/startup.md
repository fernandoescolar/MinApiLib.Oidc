[[go back to index]](../index.md#documentation-structure)

<a id="refstartup"></a>

# Startup

`MinApiLib.Oidc.Server` is a set of services, a middleware and a set of endpoints. You register the services with
`AddOidcServer`, add the middleware with `UseOidcServer` and map the endpoints with `MapOidcServer`:

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

app.MapOidcServer();

app.Run();
```

## Configuring services

`AddOidcServer` has three overloads: without arguments, with an `Action<OidcServerOptions>`, or with an
`IConfiguration` section that is bound to `OidcServerOptions`. See [options](../reference/options.md) for the options.

It registers:

* the default implementation of every service of the server;
* the cookie handlers `idsrv` and `idsrv.external`, with `idsrv` as the default scheme;
* routing, CORS, data protection, a distributed memory cache and the HTTP clients the server needs;
* an in-memory `IPersistedGrantStore` and `IDeviceFlowStore`, unless you registered your own before.

It returns an `IOidcServerBuilder`. Its extension methods, described below, register more services.

### Registering your own services

The default implementation of every service is marked with MinApiLib's `[RegisterInIServiceCollection]` attribute.
`AddOidcServer` collects them and adds each one only if the service is not registered yet. So:

* a service you register **before** `AddOidcServer` keeps your implementation;
* a service you register **after** it, as the builder's `Add*` methods do, is the one the container resolves;
* the services with several implementations (`ISecretParser`, `ISecretValidator` and options setups) are added next
  to yours.

Both of these use your profile service:

```csharp
builder.Services.AddTransient<IProfileService, UserProfileService>();
builder.Services.AddOidcServer();
```

```csharp
builder.Services.AddOidcServer()
    .AddProfileService<UserProfileService>();
```

<a id="refstartupkeymaterial"></a>

## Key material

The server supports X.509 certificates (as files or from the Windows certificate store), RSA keys and EC keys for
signing and validating tokens. Each key is used with a compatible algorithm: RS256, RS384, RS512, PS256, PS384,
PS512, ES256, ES384 or ES512.

* `AddSigningCredential`
  : Adds a signing key. Its overloads take a `SigningCredentials`, an `X509Certificate2`, the name of a certificate
    in the certificate store, a `SecurityKey` and an algorithm, or an `RsaSecurityKey` or `ECDsaSecurityKey` with an
    algorithm from `OidcServerConstants.RsaSigningAlgorithm` or `OidcServerConstants.ECDsaSigningAlgorithm`.
* `AddDeveloperSigningCredential`
  : Creates an RSA key at startup, for development. By default it writes the key to `tempkey.jwk` in the current
    directory and reuses it on the next start. The server logs a warning when it is used outside the Development
    environment.
* `AddValidationKey`
  : Adds a key that only validates tokens. It is used by the server's token validator and published in the
    discovery document.

Instead of static keys, the server can create and rotate its signing keys itself
(`OidcServerOptions.KeyManagement`). See [Cryptography, Keys and HTTPS](crypto.md#refcrypto).

## Configuration stores

The server reads clients through `IClientStore` and resources through `IResourceStore`. Startup fails if either is
missing.

### In memory

The in-memory stores take the configuration objects once, at startup. They suit configuration that lives in code or
in `appsettings.json` and rarely changes.

* `AddInMemoryClients`
  : Registers an `IClientStore` and an `ICorsPolicyService` over a collection of `Client` objects, or over a
    configuration section.
* `AddInMemoryIdentityResources`
  : Registers an `IResourceStore` over a collection of `IdentityResource` objects, or over a configuration section.
* `AddInMemoryApiScopes`
  : Registers an `IResourceStore` over a collection of `ApiScope` objects, or over a configuration section.
* `AddInMemoryApiResources`
  : Registers an `IResourceStore` over a collection of `ApiResource` objects, or over a configuration section.
* `AddInMemoryPersistedGrants`
  : Registers the in-memory `IPersistedGrantStore` and `IDeviceFlowStore`. `AddOidcServer` already calls it. The
    grants are lost on restart and are not shared between instances, so the server logs a warning when it runs with
    this store outside the Development environment.

### MongoDB

The `MinApiLib.Oidc.Server.MongoDB` package keeps clients, resources, persisted grants, device codes and signing keys
in MongoDB:

```csharp
builder.Services.AddOidcServer(options => options.KeyManagement.Enabled = true)
    .AddMongoStores(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Mongo");
        options.DatabaseName = "login";
    });
```

See [its README](../../src/MinApiLib.Oidc.Server.MongoDB/README.md) and the [MongoDB quickstart](../quickstarts/5_mongodb.md).

### Your own stores

* `AddClientStore<T>`, `AddResourceStore<T>`, `AddCorsPolicyService<T>`
  : Register your configuration stores.
* `AddPersistedGrantStore<T>`, `AddDeviceFlowStore<T>`
  : Register your stores for grants and device codes. See [Persisted Grants](persisted_grants.md#refpersistedgrants).

## Users

The server has no user store. Your login page authenticates the user and signs them in to the server's cookie, and
an `IProfileService` over your user store issues their claims. The [sample server](../../samples/Server) keeps its
users in ASP.NET Core Identity and signs them in with a password or a passkey.

## Additional services

* `AddProfileService<T>`
  : Adds an `IProfileService` that reads the user's claims from your [user store](../reference/profileservice.md).
    The default, `DefaultProfileService`, issues the claims of the authentication cookie only.
* `AddExtensionGrantValidator<T>`
  : Adds an `IExtensionGrantValidator` for an [extension grant](extension_grants.md#refextensiongrants).
* `AddResourceOwnerValidator<T>`
  : Adds an `IResourceOwnerPasswordValidator` for the deprecated
    [resource owner password grant](resource_owner.md).
* `AddSecretParser<T>`, `AddSecretValidator<T>`
  : Add an `ISecretParser` or an `ISecretValidator` for [client and API credentials](client_authentication.md).
* `AddJwtBearerClientAuthentication`
  : Adds client authentication with private key JWT assertions (`private_key_jwt`).
* `AddMutualTlsSecretValidators`
  : Adds the X.509 secret parser and validators for [mutual TLS](mtls.md).
* `AddAuthorizeInteractionResponseGenerator<T>`
  : Adds an `IAuthorizeInteractionResponseGenerator`, which decides when the authorize endpoint shows the login,
    consent or a custom page. Derive from `AuthorizeInteractionResponseGenerator` to extend the default.
* `AddCustomAuthorizeRequestValidator<T>`, `AddCustomTokenRequestValidator<T>`
  : Add your own checks of authorize and token requests. See
    [Custom Token Request Validation and Issuance](custom_token_request_validation.md).
* `AddRedirectUriValidator<T>`
  : Adds an `IRedirectUriValidator`.
* `AddAppAuthRedirectUriValidator`
  : Adds a redirect URI validator for native apps (OAuth 2.0 for Native Apps, RFC 8252): it validates strictly, but
    accepts `http://127.0.0.1` with any port.
* `AddPromptRedirect`, `AddPromptHandler<T>`
  : Add a custom `prompt` value. See [Custom prompt values](prompt.md).

## Caching

Client and resource configuration is read on most requests. If it comes from a database, cache it:

* `AddInMemoryCaching`
  : Registers `ICache<T>`, implemented over ASP.NET Core's `IMemoryCache`. The caches below need it.
* `AddClientStoreCache<T>`
  : Registers `T` as the `IClientStore`, behind a cache of `Client` objects.
* `AddResourceStoreCache<T>`
  : Registers `T` as the `IResourceStore`, behind a cache of identity resources, API resources and API scopes.
* `AddCorsPolicyCache<T>`
  : Registers `T` as the `ICorsPolicyService`, behind a cache of its answers.

`OidcServerOptions.Caching` sets how long each cache keeps its entries. To change how values are cached, replace
`ICache<T>` or `IMemoryCache` in the container.

## Configuring the pipeline

`UseOidcServer` adds the server's middleware: it keeps the session cookie in sync, runs CORS, authentication and
mutual TLS, and sends logout notifications. It calls `UseAuthentication`, so you do not need both. At startup it
checks that the stores are registered and that the options are consistent.

`MapOidcServer` maps the protocol endpoints, each one a MinApiLib endpoint record. It is required: without it, the
first request fails with an `InvalidOperationException` that says so.

```csharp
app.UseRouting();
app.UseOidcServer();

app.MapOidcServer();   // the protocol endpoints
app.MapEndpoints();    // the host's login, logout and error pages

app.Run();
```

Order matters: add the server's middleware before the endpoints of your login pages.

### Per-endpoint policies

`MapOidcServer` returns a conventions builder. Conventions added to it apply to every protocol endpoint.
`ForEndpoint` with a name from `OidcServerConstants.EndpointNames` targets one endpoint, together with its mTLS
alias:

```csharp
app.MapOidcServer()
    .ForEndpoint(OidcServerConstants.EndpointNames.Token)
    .RequireRateLimiting("token");
```

`ForEndpoint` throws for a name that is not in `OidcServerConstants.EndpointNames`. With mutual TLS in path mode, the
`/connect/mtls/...` aliases are mapped as their own endpoints and the client certificate is authenticated on them.

[[go back to index]](../index.md#documentation-structure)
