# MinApiLib.OidcServer.Client

Access tokens for `HttpClient`. Add a handler to an HTTP client and each request carries an access token, either a
**bearer** token or a **DPoP**-bound token ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) with a new proof for
each request:

- **Client credentials:** a service's own tokens. They are cached and renewed shortly before they expire.
- **The signed-in user's tokens:** the tokens of an ASP.NET Core OpenID Connect sign-in. They are kept in the
  session and refreshed with the refresh token.

It works with any OpenID Connect server, not only [MinApiLib.OidcServer](../MinApiLib.OidcServer/README.md). It
depends on ASP.NET Core and `Microsoft.IdentityModel.*`, and targets `net10.0` and `net11.0`.

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

## The signed-in user's tokens

```csharp
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("cookie")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://login.example.com";
        options.ClientId = "web";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.Scope.Add("orders.read");
        options.Scope.Add("offline_access");   // a refresh token
    });

builder.Services.AddOpenIdConnectTokens("oidc", options => options.UseDPoP = true);

builder.Services.AddHttpClient("orders", client => client.BaseAddress = new Uri("https://orders.example.com"))
    .AddUserAccessTokenHandler();
```

Then, while handling a request of the signed-in user:

```csharp
var orders = await httpClientFactory.CreateClient("orders").GetFromJsonAsync<Order[]>("/orders");
```

`AddOpenIdConnectTokens` turns on `SaveTokens` for the scheme and records in the session which scheme signed the user
in. With `UseDPoP` it also:

1. creates a key for each sign-in, when the authorization request goes out, and keeps it, protected, in the state
   and then in the session;
2. sends its thumbprint in the authorization request (`dpop_jkt`), which binds the authorization code to the key. Set
   `BindAuthorizationCode = false` for servers that reject the parameter;
3. redeems the code with a proof of the key, so the tokens are bound to it.

The handler reads the token from the session. A minute before it expires (`RefreshBeforeExpiration`), the handler
refreshes it with the refresh token, with a proof of the same key for DPoP, and writes the new tokens to the session
cookie. Concurrent requests that hold the same refresh token share one refresh, so a one-time refresh token is not
used twice. Call the API before the response starts; afterwards the cookie can no longer be updated.

`IUserAccessTokenManager` gives you the token and its key: `await tokens.GetAccessTokenAsync(HttpContext)`.

The scheme's events must be set with `options.Events`; `EventsType` is not supported. The client secret is sent in
the request body, as the OpenID Connect handler does.

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

## DPoP keys and proofs

`DPoPKey` and `DPoPProof` are public, for requests you build yourself:

```csharp
using var key = DPoPKey.CreateECDsa();         // or DPoPKey.CreateRsa() for PS256
var jwk = key.ToJwk();                          // the private key as a JWK, to store it protected
var proof = DPoPProof.Create(key, HttpMethod.Get, new Uri("https://orders.example.com/orders"), accessToken);
```

`key.Thumbprint` is the RFC 7638 thumbprint that bound tokens carry in `cnf.jkt`.

## HTTP clients

Client credentials token requests and discovery use the `HttpClient` named `OidcClientDefaults.HttpClientName`.
Configure it with `services.AddHttpClient(OidcClientDefaults.HttpClientName)` to add a proxy or a client certificate.
The user's tokens are requested through the back-channel of their OpenID Connect scheme (`BackchannelHttpHandler`).

## Samples

The [samples](../../samples/README.md) use both: the web client calls the API with the user's token, and the worker
with its own token, each as a bearer token and as a DPoP-bound token.

## License

Apache License 2.0, like MinApiLib.OidcServer. The repository's [NOTICE](../../NOTICE) holds the attribution.
