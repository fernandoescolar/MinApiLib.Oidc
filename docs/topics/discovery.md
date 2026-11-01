[[go back to index]](../index.md#documentation-structure)

# Discovery

The discovery document is at *https://baseaddress/.well-known/openid-configuration* (see the
[discovery endpoint](../endpoints/discovery.md)). It describes the endpoints, the key material and the features of
your server.

By default all information is included in the discovery document. `OidcServerOptions.Discovery`
(`DiscoveryOptions`) can hide individual sections, e.g.:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.Discovery.ShowIdentityScopes = false;
    options.Discovery.ShowApiScopes = false;
    options.Discovery.ShowClaims = false;
    options.Discovery.ShowExtensionGrantTypes = false;
});
```

The other switches are `ShowEndpoints`, `ShowKeySet` (which also turns off the `jwks` route), `ShowResponseTypes`,
`ShowResponseModes`, `ShowGrantTypes` and `ShowTokenEndpointAuthenticationMethods`. `ResponseCacheInterval` lets
clients and proxies cache the document and the key set for that many seconds.

## Extending discovery

You can add custom entries to the discovery document, e.g.:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.Discovery.CustomEntries.Add("my_setting", "foo");
    options.Discovery.CustomEntries.Add("my_complex_setting",
        new
        {
            foo = "foo",
            bar = "bar"
        });
});
```

A custom value that starts with `~/` is expanded to an absolute URL below the server's base address (unless
`ExpandRelativePathsInCustomEntries` is `false`), e.g.:

```csharp
options.Discovery.CustomEntries.Add("my_custom_endpoint", "~/custom");
```

If you want to take full control over the discovery and key set documents, implement the `IDiscoveryResponseGenerator`
interface (namespace `MinApiLib.Oidc.Server.Features.Discovery`), or derive from the default implementation,
`DiscoveryResponseGenerator`, whose `CreateDiscoveryDocumentAsync` and `CreateJwkDocumentAsync` methods are virtual.
Register it in the service collection; registered after `AddOidcServer()`, it replaces the default.

[[go back to index]](../index.md#documentation-structure)
