# MinApiLib.Oidc.Authentication

The signed-in user's access tokens for `HttpClient` in ASP.NET Core. It keeps the tokens of an OpenID Connect
sign-in in the session, refreshes them with the refresh token, and sends them with each request of an HTTP client,
either as **bearer** tokens or **DPoP**-bound ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) with a new proof
for each request.

It works with any OpenID Connect server, not only [MinApiLib.Oidc.Server](../MinApiLib.Oidc.Server/README.md). It
builds on the ASP.NET Core OpenID Connect handler and on [MinApiLib.Oidc.Http](../MinApiLib.Oidc.Http/README.md),
which sends the tokens, and targets `net10.0` and `net11.0`.

## Usage

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

## Sign-in

`AddOpenIdConnectTokens` turns on `SaveTokens` for the scheme and records in the session which scheme signed the user
in. With `UseDPoP` it also:

1. creates a key for each sign-in, when the authorization request goes out, and keeps it, protected, in the state
   and then in the session;
2. sends its thumbprint in the authorization request (`dpop_jkt`), which binds the authorization code to the key. Set
   `BindAuthorizationCode = false` for servers that reject the parameter;
3. redeems the code with a proof of the key, so the tokens are bound to it.

## Refresh

The handler reads the token from the session. A minute before it expires (`RefreshBeforeExpiration`), the handler
refreshes it with the refresh token, with a proof of the same key for DPoP, and writes the new tokens to the session
cookie. Concurrent requests that hold the same refresh token share one refresh, so a one-time refresh token is not
used twice. Call the API before the response starts; afterwards the cookie can no longer be updated.

`IUserAccessTokenManager` gives you the token and its key: `await tokens.GetAccessTokenAsync(HttpContext)`.

## Requests

Each request carries `Authorization: Bearer <token>`, or `Authorization: DPoP <token>` with a new proof. DPoP nonces
and `401` answers are handled as [MinApiLib.Oidc.Http](../MinApiLib.Oidc.Http/README.md#what-the-handlers-do-with-each-request)
describes. The user's tokens are requested through the back-channel of their OpenID Connect scheme
(`BackchannelHttpHandler`).

## Limitations

- The scheme's events must be set with `options.Events`; `EventsType` is not supported.
- The client secret is sent in the request body, as the OpenID Connect handler does.

## Samples

The [web client sample](../../samples/README.md) signs in with two schemes, one with bearer tokens and one with
DPoP-bound tokens, and calls the API with the user's token.

## License

Apache License 2.0, like MinApiLib.Oidc.Server. The repository's [NOTICE](../../NOTICE) holds the attribution.
