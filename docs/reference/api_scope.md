[[go back to index]](../index.md#documentation-structure)

# API Scope

The `ApiScope` class (namespace `MinApiLib.Oidc.Server.Core.Models`) models an OAuth scope that gives access to an
API. See [Scopes](../topics/resources.md#scopes).

`Enabled`
: Indicates if this scope is enabled and can be requested. Defaults to `true`.

`Name`
: The unique name of the scope. This is the value a client will use for the `scope` parameter in the
  authorize or token request.

`DisplayName`
: This value can be used e.g. on the consent screen.

`Description`
: This value can be used e.g. on the consent screen.

`Required`
: Specifies whether the user can de-select the scope on the consent screen (if the consent screen implements such
  a feature). A consent, at the authorize endpoint or in the device flow, that leaves out a requested required scope
  is a denial: the client gets `access_denied`. Defaults to `false`.

`Emphasize`
: Specifies whether the consent screen will emphasize this scope (if the consent screen implements such a feature).
  Use this setting for sensitive or important scopes. Defaults to `false`.

`ShowInDiscoveryDocument`
: Specifies whether this scope is shown in the discovery document. Defaults to `true`.

`UserClaims`
: List of associated user claim types that should be included in the access token.

`Properties`
: Dictionary to hold any custom scope-specific values.

## Defining API scopes in appsettings.json

`AddInMemoryApiScopes` also accepts a configuration section:

```json
"OidcServer": {
    "ApiScopes": [
        {
            "Name": "resource1.scope1"
        },
        {
            "Name": "resource2.scope1"
        },
        {
            "Name": "shared.scope"
        },
        {
            "Name": "transaction",
            "DisplayName": "Transaction",
            "Description": "A transaction"
        }
    ]
}
```

Then pass the configuration section to `AddInMemoryApiScopes`:

```csharp
builder.Services.AddOidcServer()
    .AddInMemoryApiScopes(builder.Configuration.GetSection("OidcServer:ApiScopes"));
```

[[go back to index]](../index.md#documentation-structure)
