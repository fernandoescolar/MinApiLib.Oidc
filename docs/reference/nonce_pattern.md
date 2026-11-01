[[go back to index]](../index.md#documentation-structure)

# Nonce Authorization Pattern

In some OAuth application scenarios, there may be technical barriers that prevent single sign-on (SSO) from working
as intended. End users then sign in once, and are asked to sign in again soon afterwards in another application.
When this is a blocking issue, the nonce authorization pattern can be used as a workaround.

## Mobile Web Use Case

Single sign-on may not work reliably when different applications use different cookie jars. This is often the case
for a web view, which uses a private browser session. Another example is on iOS, where a mobile app uses an
`ASWebAuthenticationSession` window and a web app usually uses the Safari browser. Cookies are not shared between
these browsers, which can prevent SSO from working.

## Insecure Solutions

One attempt to work around the problem might involve passing a token in a URL, as in the following example. In
scenarios where mobile web views are used, it is also possible to use a JavaScript interface to call from the web
view and pull a token from the mobile app.

```text
https://www.example.com/mylocation?token=abc123...
```

These solutions do not work when the web app uses HTTP-only cookies. They are also not secure designs, since tokens
should not be passed in URLs: doing so could reveal them in the browser history or server logs. Different apps
should not share tokens either, since this can lead to escalation of privileges in some cases. Each client
application must receive tokens issued to itself, with the expected scopes and claims.

## One-time Tokens

Whenever you need to pass a token in a URL, use a *nonce*: a one-time token. A nonce is safe to use in a URL
because it can only be used once and is very short lived. Even if it is revealed in the browser history or server
logs, it cannot be exploited.

Single sign-on commonly relies on cookies. If they are not sent or not available, the user is asked to sign in
again. To avoid this, the source application passes a nonce to the target application, which uses it to start the
user's session at the server.

## Creating a One-time Token

To create a nonce, the source application authenticates as a client at the [nonce endpoint](../endpoints/nonce.md)
and sends its access token as proof:

```http
POST /connect/nonce HTTP/1.1
Host: login.example.com
Authorization: Basic bW9iaWxlOnNlY3JldA==
Content-Type: application/x-www-form-urlencoded

token=eyJraWQi...
```

The server checks that:

* the client authenticates and lists the `urn:auth:nonce` grant type (`GrantType.NoncePattern`, or the
  `GrantTypes.CodeAndNonce` combination) in `AllowedGrantTypes`; otherwise it answers `invalid_client` or
  `invalid_grant`;
* the access token is valid (signature, issuer, expiry), was issued to the same client and has a `sub` claim;
* the user is still active, according to the [profile service](profileservice.md).

If all checks pass, it stores the nonce as a persisted grant and returns it. Its lifetime is the client's
`AuthorizationNonceLifetime` (120 seconds by default):

```json
{
  "nonce": "abc123...",
  "expires_in": 120
}
```

Otherwise the answer is an `invalid_token` error.

## Authenticating with a One-time Token

The target application then runs an OpenID Connect flow that includes the nonce: it sends `urn:auth:nonce` in the
`acr_values` parameter and the nonce in the `login_hint_token` parameter:

```http
GET /connect/authorize?client_id=web&
    redirect_uri=https%3A%2F%2Fweb.example.com%2Fsignin-oidc&
    response_type=code&
    scope=openid%20profile&
    code_challenge=l9QIPE4TFgW2y7STZDSWQ4Y4CQpO8W6VtELopzYHdNg&
    code_challenge_method=S256&
    state=NlAoISfdL1DxPdNG&
    login_hint_token=OFiicYQJYY2phWnD5nFMflid5Du82ycW&
    acr_values=urn:auth:nonce HTTP/1.1
Host: login.example.com
```

The authorize endpoint then:

* rejects the request with `invalid_request` if `login_hint_token` is missing, or if it has a `prompt` value other
  than `none`;
* looks up the nonce and removes it, so it cannot be used twice, and rejects the request if it is unknown or expired;
* checks with the profile service that the user is still active (`access_denied` otherwise);
* signs the user in with the nonce's subject, replacing a session of a different user, and continues the request
  without showing the login page. If the user already has a session as that subject, it is kept.

The target client does not need the `urn:auth:nonce` grant type; only the client that asks for the nonce does.

## Conclusion

The nonce authorization pattern enables single sign-on navigation from a source application to a target
application. It creates a short-lived, one-time token, with the source application's access token as proof, and the
target application uses it to start the user's session at the server. Only clients that list the `urn:auth:nonce`
grant type can create nonces.

## Example

The source application, before it opens a browser (`NonceResponse` is its own type for the JSON response):

```csharp
var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
using var request = new HttpRequestMessage(HttpMethod.Post, $"{authority}/connect/nonce")
{
    Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = accessToken })
};
request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

using var response = await httpClient.SendAsync(request);
response.EnsureSuccessStatusCode();
var nonce = await response.Content.ReadFromJsonAsync<NonceResponse>();

// open the browser at this URL
var url = $"https://web.example.com/secure?nonce={Uri.EscapeDataString(nonce!.Nonce)}";

record NonceResponse([property: JsonPropertyName("nonce")] string Nonce, [property: JsonPropertyName("expires_in")] int ExpiresIn);
```

The web application opened in the browser takes the `nonce` query string parameter and passes it on when it
redirects to the server, with the ASP.NET Core OpenID Connect handler:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://login.example.com";
        options.ClientId = "web";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.Scope.Add("profile");
        options.MapInboundClaims = false;
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (context.HttpContext.Items["nonce"] is string nonce)
            {
                context.ProtocolMessage.AcrValues = "urn:auth:nonce";
                context.ProtocolMessage.SetParameter("login_hint_token", nonce);
            }

            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Query.TryGetValue("nonce", out var nonce))
    {
        context.Items["nonce"] = nonce.ToString();

        // come back to the same URL, without the nonce
        var query = context.Request.Query
            .Where(p => p.Key != "nonce")
            .SelectMany(p => p.Value, (p, v) => new KeyValuePair<string, string?>(p.Key, v));
        var returnUrl = context.Request.PathBase + context.Request.Path + QueryString.Create(query);

        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = returnUrl });
        return;
    }

    await next(context);
});

app.UseAuthentication();
app.UseAuthorization();

// ...

app.Run();
```

[[go back to index]](../index.md#documentation-structure)
