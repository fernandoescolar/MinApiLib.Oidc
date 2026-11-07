[[go back to index]](../index.md#documentation-structure)

<a id="refidentityresource"></a>

# Identity Resource

The `IdentityResource` class (namespace `MinApiLib.Oidc.Server.Core.Models`) models an identity resource: a named
group of user claims that a client requests with the `scope` parameter. See
[Identity Resources](../topics/resources.md#identity-resources).

`Enabled`
: Indicates if this resource is enabled and can be requested. Defaults to `true`.

`Name`
: The unique name of the identity resource. This is the value a client will use for the `scope` parameter in the
  authorize request.

`DisplayName`
: This value will be used e.g. on the consent screen.

`Description`
: This value will be used e.g. on the consent screen.

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
: List of associated user claim types that should be included in the identity token. The constructors that take
  claim types require at least one.

`Properties`
: Dictionary to hold any custom resource-specific values.

## Standard identity resources

The nested classes of `IdentityResources` model the standard OpenID Connect scopes: `OpenId` (marked `Required`),
`Profile`, `Email`, `Phone` and `Address`.

```csharp
builder.Services.AddOidcServer()
    .AddInMemoryIdentityResources(
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email()
    ]);
```

`AddInMemoryIdentityResources` also accepts a configuration section, as
[API resources](api_resource.md#defining-api-resources-in-appsettingsjson) do.

[[go back to index]](../index.md#documentation-structure)
