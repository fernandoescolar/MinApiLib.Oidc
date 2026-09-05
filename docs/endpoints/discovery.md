[[go back to index]](../index.md#documentation-structure)

# Discovery Endpoint

The discovery endpoint can be used to retrieve metadata about your IdentityServer -
it returns information like the issuer name, key material, supported scopes etc. See the [spec](https://openid.net/specs/openid-connect-discovery-1_0.html) for more details.

The discovery endpoint is available via /.well-known/openid-configuration relative to the base address, e.g.:

```csharp
https://demo.identityserver.io/.well-known/openid-configuration
```

#### NOTE
You can use the [IdentityModel](https://github.com/IdentityModel/IdentityModel2) client library to programmatically access the discovery endpoint from .NET code. For more information check the IdentityModel [docs](https://identitymodel.readthedocs.io/en/latest/client/discovery.html).

[[go back to index]](../index.md#documentation-structure)