# MinApiLib.Oidc.DPoP

The building blocks of DPoP ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449), OAuth 2.0 Demonstrating Proof of
Possession), shared by the clients ([MinApiLib.Oidc.Http](../MinApiLib.Oidc.Http/README.md),
[MinApiLib.Oidc.Authentication](../MinApiLib.Oidc.Authentication/README.md)) and the server
([MinApiLib.Oidc.Server](../MinApiLib.Oidc.Server/README.md)). It only depends on
`Microsoft.IdentityModel.JsonWebTokens`, and targets `net10.0` and `net11.0`.

Most applications use it through those packages. Use it directly for requests you build yourself.

## Keys

```csharp
using var key = DPoPKey.CreateECDsa();          // ES256; or DPoPKey.CreateRsa() for PS256
var jwk = key.ToJwk();                          // the private key as a JWK, to store it protected
using var again = DPoPKey.FromJwk(jwk);
```

`key.Thumbprint` is the RFC 7638 thumbprint that bound tokens carry in `cnf.jkt` and that `dpop_jkt` sends.
`DPoPKey.ComputeThumbprint` computes it for any public JWK.

## Proofs

```csharp
var proof = DPoPProof.Create(key, HttpMethod.Get, new Uri("https://orders.example.com/orders"), accessToken, nonce);
request.Headers.Add(DPoPConstants.HeaderName, proof);
```

Each proof has a new `jti`, the method (`htm`), the URL without query or fragment (`htu`), the time (`iat`), and,
when given, the access token hash (`ath`) and the server's nonce. `DPoPProof.HashAccessToken` computes `ath`.

## Nonces

`DPoPNonceStore` keeps the last `DPoP-Nonce` each server sent, by origin, to put in the next proofs.

## Names

`DPoPConstants` has the headers, the token type, the `typ` of proofs, the `dpop_jkt` parameter, the discovery
metadata, the errors (`invalid_dpop_proof`, `use_dpop_nonce`) and the claims of the RFC.

## License

Apache License 2.0, like MinApiLib.Oidc.Server. The repository's [NOTICE](../../NOTICE) holds the attribution.
