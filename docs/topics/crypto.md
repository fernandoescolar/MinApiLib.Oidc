[[go back to index]](../index.md#documentation-structure)

# Cryptography, Keys and HTTPS

IdentityServer relies on a couple of crypto mechanisms to do its job.

## Token signing and validation

IdentityServer needs an asymmetric key pair to sign and validate JWTs.
This keymaterial can be either packaged as a certificate or just raw keys.
Both RSA and ECDSA keys are supported and the supported signing algorithms are: RS256, RS384, RS512, PS256, PS384, PS512, ES256, ES384 and ES512.

You can use multiple signing keys simultaneously, but only one signing key per algorithm is supported.
The first signing key you register is considered the default signing key.

Both [clients](../reference/client.md#refclient) and [API resources](../reference/api_resource.md#refapiresource) can express preferences on the signing algorithm.
If you request a single token for multiple API resources, all resources need to agree on at least one allowed signing algorithm.

Loading of signing key and the corresponding validation part is done by implementations of `ISigningCredentialStore` and `IValidationKeysStore`.
If you want to customize the loading of the keys, you can implement those interfaces and register them with DI.

The DI builder extensions has a couple of convenience methods to set signing and validation keys - see [here](startup.md#refstartupkeymaterial).

## Signing key rollover

While you can only use one signing key at a time, you can publish more than one validation key to the discovery document.
This is useful for key rollover.

In a nutshell, a rollover typically works like this:

1. you request/create new key material
2. you publish the new validation key in addition to the current one. You can use the `AddValidationKey` builder extension method for that.
3. all clients and APIs now have a chance to learn about the new key the next time they update their local copy of the discovery document
4. after a certain amount of time (e.g. 24h) all clients and APIs should now accept both the old and the new key material
5. keep the old key material around for as long as you like, maybe you have long-lived tokens that need validation
6. retire the old key material when it is not used anymore
7. all clients and APIs will “forget” the old key next time they update their local copy of the discovery document

This requires that clients and APIs use the discovery document, and also have a feature to periodically refresh their configuration.

Brock wrote a more detailed [blog post](https://brockallen.com/2019/08/09/identityserver-and-signing-key-rotation/) about key rotation.

## Automatic key management

This fork can create and rotate signing keys itself. It is off by default:

```csharp
services.AddIdentityServer(options =>
{
    options.KeyManagement.Enabled = true;
    options.KeyManagement.SigningAlgorithms = new[] { "RS256", "ES256" };
});
```

For each algorithm, a key is published in the discovery document as soon as it is created. It starts signing after
`PropagationTime` (14 days), signs until `RotationInterval` (90 days) and stays published for `RetentionDuration`
(14 more days) to validate the tokens it signed. It is then deleted. The next key is created `PropagationTime` before
the current one retires. At first start the new key signs immediately, because there is nothing to roll over from.

Keys are stored by `ISigningKeyStore`. The default `FileSystemSigningKeyStore` writes to `KeyManagementOptions.KeyPath`
(a `keys` folder in the current directory). The private key is encrypted with ASP.NET Core data protection
(`DataProtectKeys`), so all instances must share both the key store and the data protection key ring. Each instance
reloads keys every `KeyCacheDuration` (one hour).

Static credentials added with `AddSigningCredential` are still published. Automatic keys sign for the algorithms they
cover, and static credentials sign for any other algorithm.

## Data protection

Cookie authentication in ASP.NET Core (or anti-forgery in MVC) use the ASP.NET Core data protection feature.
Depending on your deployment scenario, this might require additional configuration. See the Microsoft [docs](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview) for more information.

## HTTPS

We don’t enforce the use of HTTPS, but for production it is mandatory for every interaction with IdentityServer.

[[go back to index]](../index.md#documentation-structure)
