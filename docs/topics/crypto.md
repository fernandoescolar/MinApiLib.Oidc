[[go back to index]](../index.md#documentation-structure)

<a id="refcrypto"></a>

# Cryptography, Keys and HTTPS

The server relies on a couple of crypto mechanisms to do its job.

## Token signing and validation

The server needs an asymmetric key pair to sign and validate JWTs. The key material can be a certificate or raw keys.
Both RSA and ECDSA keys are supported, with the signing algorithms RS256, RS384, RS512, PS256, PS384, PS512, ES256,
ES384 and ES512.

You can use several signing keys at once, but only one per algorithm. The first one you register is the default
signing key.

Both [clients](../reference/client.md) and [API resources](../reference/api_resource.md) can express preferences on
the signing algorithm (`AllowedIdentityTokenSigningAlgorithms` on the client, `AllowedAccessTokenSigningAlgorithms` on
the API resource). If you request a single token for several API resources, they need to agree on at least one
algorithm.

The keys are loaded by implementations of `ISigningCredentialStore` and `IValidationKeysStore` (namespace
`MinApiLib.Oidc.Server.Core.Keys`). `IKeyMaterialService` combines them with the automatic keys described below. To
load keys your own way, implement the stores and register them in the container.

The builder has methods to add static signing and validation keys: `AddSigningCredential`,
`AddDeveloperSigningCredential` and `AddValidationKey`. See [here](startup.md#refstartupkeymaterial).

```csharp
builder.Services.AddOidcServer()
    .AddSigningCredential(X509CertificateLoader.LoadPkcs12FromFile("signing.pfx", builder.Configuration["SigningKeyPassword"]));
```

`AddDeveloperSigningCredential` creates an RSA key and keeps it in `tempkey.jwk`. Use it only for development: the
server logs a warning when it is used outside the Development environment.

## Signing key rollover

While you can only sign with one key per algorithm at a time, you can publish more than one validation key in the
discovery document. This is how keys are rolled over by hand:

1. create the new key material;
2. publish the new validation key next to the current one, with `AddValidationKey`;
3. clients and APIs learn about the new key the next time they refresh their copy of the discovery document;
4. after some time (for example 24 hours), all clients and APIs accept both the old and the new key;
5. sign with the new key, and keep publishing the old one for as long as tokens it signed can still be in use;
6. retire the old key;
7. clients and APIs forget the old key the next time they refresh their copy of the discovery document.

This requires that clients and APIs use the discovery document and refresh it periodically. Automatic key management
does all of this for you.

## Automatic key management

The server can create and rotate its signing keys itself. It is off by default:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.KeyManagement.Enabled = true;
    options.KeyManagement.SigningAlgorithms = ["RS256", "ES256"];
});
```

`OidcServerOptions.KeyManagement` is a `KeyManagementOptions`:

| Option | Default | |
|---|---|---|
| `Enabled` | `false` | Create and rotate signing keys |
| `SigningAlgorithms` | RS256 | One key per algorithm; the first is the default |
| `RotationInterval` | 90 days | How long a key signs, counted from its creation |
| `PropagationTime` | 14 days | How long a new key is published before it signs |
| `RetentionDuration` | 14 days | How long a retired key stays published |
| `DeleteRetiredKeys` | `true` | Delete keys from the store after their retention |
| `KeyCacheDuration` | 1 hour | How long each instance caches the keys it loaded |
| `DataProtectKeys` | `true` | Encrypt the private keys with ASP.NET Core data protection |
| `KeyPath` | `./keys` | Directory of the default file system store |

For each algorithm, a key is published in the discovery document as soon as it is created. It starts signing after
`PropagationTime`, signs until `RotationInterval` and then stays published for `RetentionDuration` to validate the
tokens it signed. The next key is created `PropagationTime` before the current one retires. At first start the new
key signs immediately, because there is nothing to roll over from.

Keys are kept in an `ISigningKeyStore`, which every instance of the server must share:

* `FileSystemSigningKeyStore`, the default, writes one JSON file per key to `KeyPath`. It suits a single instance or
  a shared volume.
* `MinApiLib.Oidc.Server.MongoDB` keeps the keys in its `signingKeys` collection when `AddMongoStores` is called
  (`MongoStoreOptions.UseForSigningKeys`, on by default).
* `InMemorySigningKeyStore` loses the keys on restart; use it only for tests.
* Your own: implement `LoadKeysAsync`, `StoreKeyAsync` and `DeleteKeyAsync` and register it as the
  `ISigningKeyStore`.

With `DataProtectKeys`, all instances must also share the data protection key ring; a key protected with another key
ring cannot be loaded, and the server logs an error. Each instance reloads the keys every `KeyCacheDuration`, which must
be shorter than `PropagationTime`. Inconsistent `KeyManagement` options stop the host from starting.

Static credentials added with `AddSigningCredential` are still published. Automatic keys sign for the algorithms they
cover, and static credentials sign for any other algorithm.

## Data protection

Cookie authentication and antiforgery in ASP.NET Core use the ASP.NET Core data protection feature, and so do
several parts of the server (the protected private keys above, among others). With several instances, or keys that
must survive a restart, the key ring needs configuration. See the Microsoft
[docs](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview).

## HTTPS

The server does not enforce HTTPS, but in production it is mandatory for every interaction with it.

[[go back to index]](../index.md#documentation-structure)
