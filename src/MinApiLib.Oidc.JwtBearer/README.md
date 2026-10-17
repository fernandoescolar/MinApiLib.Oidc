# MinApiLib.Oidc.JwtBearer

DPoP ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) for APIs that use ASP.NET Core JWT bearer authentication.
One line after `AddJwtBearer` and the API accepts DPoP-bound access tokens with their proofs, and stops a stolen
DPoP-bound token from working as a bearer token.

It works with any OAuth 2.0 server that issues DPoP-bound JWT access tokens (`cnf.jkt`), not only
[MinApiLib.Oidc.Server](../MinApiLib.Oidc.Server/README.md). It builds on `Microsoft.AspNetCore.Authentication.JwtBearer`
and [MinApiLib.Oidc.DPoP](../MinApiLib.Oidc.DPoP/README.md), and targets `net10.0` and `net11.0`.

## Usage

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.example.com";
        options.Audience = "orders";
    })
    .AddDPoP();
```

In an endpoint, `HttpContext.GetAccessTokenScheme()` says whether the token came as `Bearer` or `DPoP`, and
`HttpContext.GetDPoPThumbprint()` gives the key the request proved it holds.

## What it checks

| Request | Answer |
|---|---|
| `Authorization: DPoP <token>` with a valid proof of the key in `cnf.jkt` | Authenticated |
| `Authorization: Bearer <token>` of a bearer token | Authenticated, unless `Mode = DPoPTokenMode.Required` |
| `Authorization: Bearer <token>` of a DPoP-bound token | `401`, `invalid_token` |
| `Authorization: DPoP <token>` of a bearer token | `401`, `invalid_token` |
| A proof that is missing, malformed, signed by a key not in its header, for another method or URL, too old, without the token hash (`ath`) or already used | `401`, `invalid_dpop_proof` |
| A valid proof of another key | `401`, `invalid_token` |
| With `RequireNonce`, a proof without a nonce the API issued | `401`, `use_dpop_nonce` and a new nonce in `DPoP-Nonce` |

The challenge announces both schemes, `Bearer` (as `options.Challenge` of the JWT bearer scheme says) and
`DPoP algs="ES256 PS256"`, with the error on the scheme the request used. With `Mode = DPoPTokenMode.Required` it only
announces `DPoP`.

## Options

| Option | Default | |
|---|---|---|
| `Mode` | `Allowed` | `Required` accepts DPoP-bound tokens only |
| `SupportedAlgorithms` | ES256, PS256 | Accepted proof algorithms |
| `ProofLifetime`, `ClockSkew` | 1 minute each | How old a proof can be |
| `RequireNonce` | `false` | Proofs must carry a nonce of the API (RFC 9449 section 9) |
| `NonceLifetime` | 5 minutes | How long a nonce is accepted |

Nonces are encrypted with ASP.NET Core Data Protection, so the API stores nothing; instances that share the Data
Protection keys accept each other's nonces. [MinApiLib.Oidc.Http](../MinApiLib.Oidc.Http/README.md) and
[MinApiLib.Oidc.Authentication](../MinApiLib.Oidc.Authentication/README.md) answer `use_dpop_nonce` on their own.

## Replay cache

Each proof is accepted once. The proofs seen are kept in an `IDPoPReplayCache`, in memory by default. An API on
several instances needs a shared one:

```csharp
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = "...");
builder.Services.AddSingleton<IDPoPReplayCache, DistributedDPoPReplayCache>();
```

## Limitations

- The scheme's events must be set with `options.Events`; `EventsType` is not supported. A `JwtBearerEvents`
  subclass that overrides a method must call the base method.
- The request URL is built from the request's scheme, host and path. Behind a proxy, use the forwarded headers
  middleware so it matches the URL the client called.

## Samples

The [API sample](../../samples/README.md) uses it, and the web client's buttons show what it rejects.

## License

Apache License 2.0, like MinApiLib.Oidc.Server. The repository's [NOTICE](../../NOTICE) holds the attribution.
