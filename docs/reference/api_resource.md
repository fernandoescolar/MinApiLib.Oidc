[[go back to index]](../index.md#documentation-structure)

<a id="refapiresource"></a>

# API Resource

The `ApiResource` class (namespace `MinApiLib.Oidc.Server.Core.Models`) models an API resource. See
[API Resources](../topics/resources.md#api-resources).

`Enabled`
: Indicates if this resource is enabled and can be requested. Defaults to `true`.

`Name`
: The unique name of the API. This value is used for authentication with introspection and will be added to the
  audience of the outgoing access token.

`DisplayName`
: This value can be used e.g. on the consent screen.

`Description`
: This value can be used e.g. on the consent screen.

`ShowInDiscoveryDocument`
: Specifies whether this resource is shown in the discovery document. Defaults to `true`.

`ApiSecrets`
: The API secrets are used for the introspection endpoint. The API can authenticate with introspection using the
  API name and a secret.

`AllowedAccessTokenSigningAlgorithms`
: List of allowed signing algorithms for the access token. If empty, the server's default signing algorithm is used.

`UserClaims`
: List of associated user claim types that should be included in the access token.

`Scopes`
: List of API scope names.

`RequireResourceIndicator`
: When `true`, the API is added to the audience of an access token only if the client asks for it with a
  `resource` parameter (RFC 8707). Defaults to `false`: the API is included whenever one of its scopes is granted.
  See [Resource indicators](../topics/resources.md#resource-indicators).

`Properties`
: Dictionary to hold any custom resource-specific values.

## Defining API resources in appsettings.json

`AddInMemoryApiResources` also accepts a configuration section:

```json
"OidcServer": {
    "ApiResources": [
        {
            "Name": "resource1",
            "DisplayName": "Resource #1",

            "Scopes": [
                "resource1.scope1",
                "shared.scope"
            ]
        },
        {
            "Name": "resource2",
            "DisplayName": "Resource #2",
            "RequireResourceIndicator": true,

            "UserClaims": [
                "name",
                "email"
            ],

            "Scopes": [
                "resource2.scope1",
                "shared.scope"
            ]
        }
    ]
}
```

Then pass the configuration section to `AddInMemoryApiResources`:

```csharp
builder.Services.AddOidcServer()
    .AddInMemoryApiResources(builder.Configuration.GetSection("OidcServer:ApiResources"));
```

[[go back to index]](../index.md#documentation-structure)
