[[go back to index]](../index.md#documentation-structure)

# ASP.NET Core and API access

The previous quickstarts covered API access and user authentication separately. This one brings them together: the
web application calls the API with the signed-in user's access token.

OpenID Connect and OAuth 2.0 do both in one exchange. Once the client also asks for an API scope, the token endpoint
returns two tokens: the identity token, with the outcome of the authentication, and an access token to call the API
on behalf of the user. With the `offline_access` scope it also returns a refresh token, to get new access tokens
without sending the user back to the server.

Do [quickstart 1](1_client_credentials.md) and [quickstart 2](2_interactive_aspnetcore.md) first. The finished code
is in [samples/](../../samples/README.md): `Server`, `Api` and `WebClient`.

## Modifying the client configuration

On the server, allow the web client to ask for the API scope and for a refresh token (`AllowOfflineAccess`):

```csharp
new Client
{
    ClientId = "web",
    // ... as in quickstart 2

    AllowOfflineAccess = true,
    AllowedScopes = { "openid", "profile", "email", "orders.read" }
}
```

Optionally, put the user's name in the access token, for the API, with the API resource's `UserClaims`:

```csharp
new ApiResource("orders", "Orders API") { Scopes = { "orders.read" }, UserClaims = { "name" } }
```

## Modifying the web client

Ask for the API scope and for a refresh token in the OpenID Connect handler:

```csharp
.AddOpenIdConnect("oidc", options =>
{
    // ... as in quickstart 2
    options.Scope.Add("email");
    options.Scope.Add("orders.read");
    options.Scope.Add("offline_access");
});
```

Because `SaveTokens` is on, the handler keeps the access token, the refresh token and the identity token in the
authentication session, the application's cookie. You could read the access token with
`await HttpContext.GetTokenAsync("access_token")` and set it on an `HttpClient` yourself, but then you also have to
refresh it before it expires and write the new tokens back to the cookie. `MinApiLib.Oidc.Authentication`, which the
web client already references, does that.

## Using the access token

Register the tokens of the `oidc` scheme, and an HTTP client for the API that sends them. Add this to
`WebClient/Program.cs`, before `builder.Build()`:

```csharp
// keep the tokens of the "oidc" sign-in in the session, and refresh them
builder.Services.AddOpenIdConnectTokens("oidc");

// the API: each request carries the signed-in user's access token
builder.Services.AddHttpClient("orders", client => client.BaseAddress = new Uri("https://localhost:5003"))
    .AddUserAccessTokenHandler();
```

and an endpoint that calls the API:

```csharp
app.MapGet("/call-api", async (IHttpClientFactory clients) =>
    Results.Content(await clients.CreateClient("orders").GetStringAsync("/orders"), "application/json"))
    .RequireAuthorization();
```

* `AddOpenIdConnectTokens("oidc")` takes the name of the OpenID Connect scheme. It turns on `SaveTokens` for it and
  records in the session which scheme signed the user in.
* `AddUserAccessTokenHandler()` adds the handler that reads the user's access token from the session and sends it with
  each request. It reads the session of the default authenticate scheme, here `cookie`; pass the scheme name if your
  session lives elsewhere.

Run the server, the API and the web client. Sign in at `https://localhost:5002`, then open
`https://localhost:5002/call-api`. The web client sends:

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6...
```

and the API answers with the user and the client of the token:

```json
{
  "client": "web",
  "user": "1",
  "orders": [ ... ]
}
```

To get the token itself, for example to show it, use `IUserAccessTokenManager` (namespace
`MinApiLib.Oidc.Authentication`): `await tokens.GetAccessTokenAsync(HttpContext)`.

## Managing the access token

The hard part of a client is managing the access token:

* request the access and refresh tokens at sign-in;
* keep them;
* use the access token until it expires;
* use the refresh token to get a new access token;
* start over.

The handler does all of it. The access token lasts an hour (`Client.AccessTokenLifetime`). A minute before it
expires (`RefreshBeforeExpiration`), the handler refreshes it with the refresh token, at the server's token endpoint,
and writes the new tokens to the session cookie. Concurrent requests that hold the same refresh token share one
refresh, so a one-time refresh token is not used twice. If the API answers `401`, the handler gets a new token and
sends the request once more.

Call the API before your response starts: once the response has started, the cookie can no longer be updated.

On the server, refresh tokens are one-time only by default (`Client.RefreshTokenUsage = TokenUsage.OneTimeOnly`): each
refresh returns a new refresh token, and reusing an old one revokes the user's refresh tokens for that client. They
last 30 days (`AbsoluteRefreshTokenLifetime`). See [Refresh Tokens](../topics/refresh_tokens.md).

## Binding the user's token to a key with DPoP

As with the worker in [quickstart 1](1_client_credentials.md#binding-the-token-to-a-key-with-dpop), the user's tokens
can be DPoP-bound, so a stolen access token or refresh token is useless without the key. The API already accepts
DPoP-bound tokens if it has `.AddDPoP()`. In the web client, change one line:

```csharp
builder.Services.AddOpenIdConnectTokens("oidc", options => options.UseDPoP = true);
```

With `UseDPoP`, the library:

1. creates a P-256 key for each sign-in, when the authorization request goes out, and keeps it, protected, in the
   state and then in the session;
2. sends the key's thumbprint in the authorization request (`dpop_jkt`), which binds the authorization code to the key;
3. redeems the code with a proof of the key: the server answers `"token_type": "DPoP"` and the access token carries the
   thumbprint in `cnf.jkt`;
4. sends each API call with the `DPoP` scheme and a new proof, and refreshes the tokens with a proof of the same key.

```http
GET /orders HTTP/1.1
Host: localhost:5003
Authorization: DPoP eyJhbGciOiJSUzI1NiIsImtpZCI6...
DPoP: eyJhbGciOiJFUzI1NiIsImp3ayI6eyJrdHkiOiJFQyIs...
```

Keep `GetClaimsFromUserInfoEndpoint = false`: the userinfo endpoint also requires a proof for a DPoP-bound token, and
the OpenID Connect handler does not send one. The claims come in the identity token instead
(`AlwaysIncludeUserClaimsInIdToken`).

To make DPoP mandatory for the client, set `RequireDPoP = true` on it in the server: the token endpoint then refuses
requests without a proof.

The sample web client registers two OpenID Connect schemes side by side, `web` (bearer) and `web.dpop` (DPoP), and
has buttons that play an attacker who stole the token. [Proof-of-Possession Access Tokens](../topics/pop.md) walks
through the exchange message by message.

[[go back to index]](../index.md#documentation-structure)
