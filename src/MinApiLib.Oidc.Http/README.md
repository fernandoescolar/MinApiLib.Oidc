# MinApiLib.Oidc.Http

Access tokens for `HttpClient`, without ASP.NET Core. Add a handler to an HTTP client and each request carries the
client's own access token (client credentials grant), either a **bearer** token or a **DPoP**-bound token
([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) with a new proof for each request.

It works with any OAuth 2.0 server, not only [MinApiLib.Oidc.Server](../MinApiLib.Oidc.Server/README.md). It
depends on `Microsoft.Extensions.Http`, `Microsoft.IdentityModel.Protocols.OpenIdConnect` and
[MinApiLib.Oidc.DPoP](../MinApiLib.Oidc.DPoP/README.md), and targets `net10.0` and `net11.0`, so it fits workers,
console applications and web applications alike.

For the tokens of a user signed in with OpenID Connect in ASP.NET Core, use
[MinApiLib.Oidc.Authentication](../MinApiLib.Oidc.Authentication/README.md), which builds on this package.

## Client credentials

```csharp
builder.Services.AddClientCredentialsTokens("orders", options =>
{
    options.Authority = "https://login.example.com";
    options.ClientId = "worker";
    options.ClientSecret = "secret";
    options.Scope = "orders.read";
    options.UseDPoP = true;   // false, the default: bearer tokens
});

builder.Services.AddHttpClient("orders", client => client.BaseAddress = new Uri("https://orders.example.com"))
    .AddClientCredentialsTokenHandler("orders");
```

```csharp
var orders = await httpClientFactory.CreateClient("orders").GetFromJsonAsync<Order[]>("/orders");
```

The handler finds the token endpoint in the discovery document, or uses `TokenEndpoint` if you set it. It asks for a
token on the first request and reuses it until a minute before it expires (`RenewBeforeExpiration`). Concurrent
requests share one token request. With `UseDPoP`, the client creates an EC P-256 key in memory, or uses
`DPoPJsonWebKey` if you set it, and every token request and API call carries a proof of it.

`IClientCredentialsTokenManager` gives you the token directly:
`await tokens.GetAccessTokenAsync("orders")`.

## What the handlers do with each request

| Token | Request |
|---|---|
| Bearer | `Authorization: Bearer <token>` |
| DPoP | `Authorization: DPoP <token>` and `DPoP: <proof>`: a new proof for the method and URL, with the token hash (`ath`) |

When the token endpoint or the API answers `use_dpop_nonce` with a `DPoP-Nonce` header (RFC 9449, sections 8 and 9),
the handler sends the request once more with the nonce in the proof. It keeps the nonce of each server for the next
proofs. When the API answers `401` for another reason, the handler gets a new token and sends the request once more.
Resending works with content that can be read twice, such as `StringContent`, `ByteArrayContent` or `JsonContent`, but
not with `StreamContent`.

If the server answers `token_type: Bearer` to a DPoP request, because it does not support DPoP, the token is sent as a
bearer token.

Errors of the token endpoint throw `AccessTokenException`, with the OAuth `Error` and `ErrorDescription`.

## Your own handlers

The handlers derive from `AccessTokenHandler`, which does all of the above; a derived class only says where the token
comes from. `TokenEndpointClient` posts token requests with a proof and the nonce retry. Register both with
`services.AddTokenEndpointClient()`:

```csharp
sealed class VaultTokenHandler(IVault vault, DPoPNonceStore nonces, TimeProvider time, ILogger<VaultTokenHandler> logger)
    : AccessTokenHandler(nonces, time, logger)
{
    protected override Task<AccessToken> GetAccessTokenAsync(bool forceRenewal, CancellationToken cancellationToken)
        => vault.GetTokenAsync(forceRenewal, cancellationToken);
}
```

## HTTP clients

Token requests and discovery use the `HttpClient` named `OidcHttpDefaults.HttpClientName`. Configure it with
`services.AddHttpClient(OidcHttpDefaults.HttpClientName)` to add a proxy or a client certificate.

## Samples

The [worker sample](../../samples/README.md) calls the API with its own token, as a bearer token and as a DPoP-bound
token.

## License

Apache License 2.0, like MinApiLib.Oidc.Server. The repository's [NOTICE](../../NOTICE) holds the attribution.
