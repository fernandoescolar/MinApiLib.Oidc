[[go back to index]](../index.md#documentation-structure)

# Startup

IdentityServer is a combination of middleware and services.
All configuration is done in your startup class.

## Configuring services

You add the IdentityServer services to the DI system by calling:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var builder = services.AddIdentityServer();
}
```

Optionally you can pass in options into this call. See [here](../reference/options.md#refoptions) for details on options.

This will return you a builder object that in turn has a number of convenience methods to wire up additional services.

<a id="refstartupkeymaterial"></a>

## Key material

IdentityServer supports X.509 certificates (both raw files and a reference to the Windows certificate store),
RSA keys and EC keys for token signatures and validation. Each key can be configured with a (compatible) signing algorithm,
e.g. RS256, RS384, RS512, PS256, PS384, PS512, ES256, ES384 or ES512.

You can configure the key material with the following methods:

* `AddSigningCredential`
  : Adds a signing key that provides the specified key material to the various token creation/validation services.
* `AddDeveloperSigningCredential`
  : Creates temporary key material at startup time. This is for dev scenarios. The generated key will be persisted in the local directory by default.
* `AddValidationKey`
  : Adds a key for validating tokens. They will be used by the internal token validator and will show up in the discovery document.

## In-Memory configuration stores

The various “in-memory” configuration APIs allow for configuring IdentityServer from an in-memory list of configuration objects.
These “in-memory” collections can be hard-coded in the hosting application, or could be loaded dynamically from a configuration file or a database.
By design, though, these collections are only created when the hosting application is starting up.

Use of these configuration APIs are designed for use when prototyping, developing, and/or testing where it is not necessary to dynamically consult database at runtime for the configuration data.
This style of configuration might also be appropriate for production scenarios if the configuration rarely changes, or it is not inconvenient to require restarting the application if the value must be changed.

* `AddInMemoryClients`
  : Registers `IClientStore` and `ICorsPolicyService` implementations based on the in-memory collection of `Client` configuration objects.
* `AddInMemoryIdentityResources`
  : Registers `IResourceStore` implementation based on the in-memory collection of `IdentityResource` configuration objects.
* `AddInMemoryApiScopes`
  : Registers `IResourceStore` implementation based on the in-memory collection of `ApiScope` configuration objects.
* `AddInMemoryApiResources`
  : Registers `IResourceStore` implementation based on the in-memory collection of `ApiResource` configuration objects.

## Test stores

The `TestUser` class models a user, their credentials, and claims in IdentityServer.
Use of `TestUser` is similar to the use of the “in-memory” stores in that it is intended for when prototyping, developing, and/or testing.
The use of `TestUser` is not recommended in production.

* `AddTestUsers`
  : Registers `TestUserStore` based on a collection of `TestUser` objects.
    `TestUserStore` is used by the default quickstart UI.
    Also registers implementations of `IProfileService` and `IResourceOwnerPasswordValidator`.

## Additional services

* `AddExtensionGrantValidator`
  : Adds `IExtensionGrantValidator` implementation for use with extension grants.
* `AddSecretParser`
  : Adds `ISecretParser` implementation for parsing client or API resource credentials.
* `AddSecretValidator`
  : Adds `ISecretValidator` implementation for validating client or API resource credentials against a credential store.
* `AddResourceOwnerValidator`
  : Adds `IResourceOwnerPasswordValidator` implementation for validating user credentials for the resource owner password credentials grant type.
* `AddProfileService`
  : Adds `IProfileService` implementation for connecting to your [custom user profile store](../reference/profileservice.md#refprofileservice).
    The `DefaultProfileService` class provides the default implementation which relies upon the authentication cookie as the only source of claims for issuing in tokens.
* `AddAuthorizeInteractionResponseGenerator`
  : Adds `IAuthorizeInteractionResponseGenerator` implementation to customize logic at authorization endpoint for when a user must be shown a UI for error, login, consent, or any other custom page.
    The `AuthorizeInteractionResponseGenerator` class provides a default implementation, so consider deriving from this existing class if you need to augment the existing behavior.
* `AddCustomAuthorizeRequestValidator`
  : Adds `ICustomAuthorizeRequestValidator` implementation to customize request parameter validation at the authorization endpoint.
* `AddCustomTokenRequestValidator`
  : Adds `ICustomTokenRequestValidator` implementation to customize request parameter validation at the token endpoint.
* `AddRedirectUriValidator`
  : Adds `IRedirectUriValidator` implementation to customize redirect URI validation.
* `AddAppAuthRedirectUriValidator`
  : Adds a an “AppAuth” (OAuth 2.0 for Native Apps) compliant redirect URI validator (does strict validation but also allows [http://127.0.0.1](http://127.0.0.1) with random port).
* `AddJwtBearerClientAuthentication`
  : Adds support for client authentication using JWT bearer assertions.
* `AddMutualTlsSecretValidators`
  : Adds the X509 secret validators for mutual TLS.

## Caching

Client and resource configuration data is used frequently by IdentityServer.
If this data is being loaded from a database or other external store, then it might be expensive to frequently re-load the same data.

* `AddInMemoryCaching`
  : To use any of the caches described below, an implementation of `ICache<T>` must be registered in DI.
    This API registers a default in-memory implementation of `ICache<T>` that’s based on ASP.NET Core’s `MemoryCache`.
* `AddClientStoreCache`
  : Registers a `IClientStore` decorator implementation which will maintain an in-memory cache of `Client` configuration objects.
    The cache duration is configurable on the `Caching` configuration options on the `IdentityServerOptions`.
* `AddResourceStoreCache`
  : Registers a `IResourceStore` decorator implementation which will maintain an in-memory cache of `IdentityResource` and `ApiResource` configuration objects.
    The cache duration is configurable on the `Caching` configuration options on the `IdentityServerOptions`.
* `AddCorsPolicyCache`
  : Registers a `ICorsPolicyService` decorator implementation which will maintain an in-memory cache of the results of the CORS policy service evaluation.
    The cache duration is configurable on the `Caching` configuration options on the `IdentityServerOptions`.

Further customization of the cache is possible:

The default caching relies upon the `ICache<T>` implementation.
If you wish to customize the caching behavior for the specific configuration objects, you can replace this implementation in the dependency injection system.

The default implementation of the `ICache<T>` itself relies upon the `IMemoryCache` interface (and `MemoryCache` implementation) provided by .NET.
If you wish to customize the in-memory caching behavior, you can replace the `IMemoryCache` implementation in the dependency injection system.

## Configuring the pipeline

The protocol endpoints are served through ASP.NET Core endpoint routing. Add the IdentityServer middleware with
`UseIdentityServer` and map the endpoints with `MapIdentityServer`:

```csharp
var app = builder.Build();

app.UseIdentityServer();
app.UseAuthorization();

app.MapIdentityServer();
app.MapRazorPages();

app.Run();
```

`UseIdentityServer` keeps the session cookie in sync, runs CORS, authentication and mutual TLS, and sends logout
notifications; it includes a call to `UseAuthentication`, so it's not necessary to have both. `MapIdentityServer`
maps every registered endpoint, including the ones added with `AddEndpoint<T>`. `AddIdentityServer` registers the
routing services. A host that calls `UseIdentityServer` without `MapIdentityServer` fails on the first request with
an `InvalidOperationException` that says so.

#### NOTE
The middleware-based router of IdentityServer4 (`IEndpointRouter`) has been removed: endpoint routing is the only
way to serve the endpoints.

Be aware that order matters in the pipeline.
For example, you will want to add IdentityServer before the UI framework that implements the login screen.

### Per-endpoint policies

`MapIdentityServer` returns a builder for conventions. Use `ForEndpoint` with a name from
`IdentityServerConstants.EndpointNames` to target one endpoint:

```csharp
app.MapIdentityServer()
    .ForEndpoint(IdentityServerConstants.EndpointNames.Token)
    .RequireRateLimiting("token");
```

With mutual TLS enabled in path mode, the `/connect/mtls/...` aliases are mapped as their own endpoints and the
client certificate is authenticated on them.

[[go back to index]](../index.md#documentation-structure)
