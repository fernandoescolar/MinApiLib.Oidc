[[go back to index]](../index.md#documentation-structure)

# Nonce Authorization Pattern

In some OAuth application scenarios, there may be technical barriers that prevent a Single Sign-On (SSO) solution from working as intended. This can lead to end users having to perform an initial login, then being prompted very soon afterwards to sign in again. When software companies consider this a blocking issue and need a workaround, the nonce authenticator pattern can be used.

## Mobile Web Use Case

Single Sign-On may not work reliably when different applications use different cookie jars. This is often the case for a web view, which uses a private browser session. Another example is on iOS, where a mobile app uses an ASWebAuthenticationSession window, and a web app usually uses the Safari browser. Cookies are not shared between these browsers, which can prevent SSO from working.

## Insecure Solutions

One attempt to work around the problem might involve passing a token in a URL, as in the following example. In scenarios where mobile web views are used, it is also possible to use a Javascript Interface, to call from the web view and pull a token from the mobile app.

```text
https://www.example.com/mylocation?token=abc123...
```

These solutions will not work when the web app uses HTTP-only cookies. They are also not secure designs, since tokens should not be passed in URLs. Doing so could reveal them in the browser history or server logs. Different apps should not share tokens, since this can lead to escalation of privileges in some cases. Instead, each client application must receive tokens issued to itself, with the expected scopes and claims.

## One-time Tokens

Whenever you need to pass tokens in URLs, use a *nonce* as a one-time token. Your authorization server should provide a flow for issuing these to applications. A *nonce* is safe to use in a URL because it is a number that can only be used once, and is very short lived. So even if a *nonce* is revealed in the browser history or server logs, it cannot be exploited.

Traditionally, a client makes use of SSO to seamlessly authenticate and obtain tokens. However, SSO commonly relies on cookies and if for some reason these are not sent or accessible, the user will be reprompted for authentication. To avoid this, pass a one-time (authentication) token, the *nonce*.

## Creating a One-time Token

To create a nonce, the source application sends its *access_token* as a proof, to the nonce issuing endpoint of the authorization server.

The following POST shows the input in an example request to get a nonce:

```http
POST /connect/nonce HTTP/1.1
Host: identity.server
Authorization: Basic AO93kK...
Content-Type: application/x-www-form-urlencoded

token=eyJraWQi...
```

The nonce issuing endpoint will cryptographically verify the signature of the *access_token*, then check its issuer, audience and expiry. Then it will check the client ID from the token against the client ID of the application requesting the nonce (in the *Authorization* header). This client should allow nonce (`urn:auth:nonce`) grant type. If all checks pass, the nonce is issued:

```json
{
  "nonce": "abc123...",
  "expires_in": 100
}
```

## Authenticating with a One-time Token

The target application then needs to run an OpenID Connect flow that includes the nonce. The authentication request will use the **acr_values** field to ensure that the nonce authenticator is used. It will also send the nonce in the **login_hint_token** field, and prompt=login to ensure that the nonce is used:

```http
GET /connect/authorize HTTP/1.1
Host: identity.server
Content-Type: application/x-www-form-urlencoded

client_id=web-client&
redirect_uri=http%3A%2F%2Fwww.example.com%2F&
response_type=code&
code_challenge=l9QIPE4TFgW2y7STZDSWQ4Y4CQpO8W6VtELopzYHdNg&
code_challenge_method=S256&
state=NlAoISfdL1DxPdNGFBljlVuB1GDjgGARmqDcxtHhV8iKNYu6ECS2KOavDHpI3eLN&
scope=openid%20profile&
login_hint_token=OFiicYQJYY2phWnD5nFMflid5Du82ycW&
acr_values=urn:auth:nonce
```

This redirect will be to the authorization endpoint of the authorization server, after which a temporary cookie is set and there is a second redirect to the nonce authenticator.

## Conclusion

The nonce authorization pattern enables Single Sign-On navigation from a source application to a target application. It does so by creating a short-lived one-time token, using the *acess_token* of the source application as a proof. This *nonce* is then used to bootstrap a session in the target application. Usage should be restricted to specific clients, who allow the grant type `urn:auth:nonce`.

## Example

Client before opening a browser:

```csharp
var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
var nonceRequest = new HttpRequestMessage(HttpMethod.Post, $"{authority}/connect/nonce")
{
    Content = new FormUrlEncodedContent(new Dictionary<string, string> { { "token", AccessToken } })
};
nonceRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
var nonceResponse = await new HttpClient().SendAsync(nonceRequest);
var nonce = await nonceResponse.Content.ReadAsStringAsync();
var res = JsonSerializer.Deserialize<NonceResponse>(nonce);

// open browser using the following URL
Console.WriteLine($"https://yourweb.com/secure?nonce={res.nonce}");
```

Web that is opened in the browser shold handle the `nonce` query string parameter before redirecting to the identity provider:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(op =>
    {
        op.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        op.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(op =>
    {
        op.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        op.Authority = "https://auth.example.com";
        op.ClientId = ClientId;
        op.ClientSecret = ClientSecret;
        op.ResponseType = "code";
        op.ResponseMode = "query";
        op.UsePkce = true;
        op.Scope.Add("openid");
        op.Scope.Add("profile");
        op.Scope.Add("offline_access");
        op.MapInboundClaims = false;
        op.GetClaimsFromUserInfoEndpoint = false;
        op.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = ctx =>
            {
                if (ctx.Request.HttpContext.Items.ContainsKey("nonce"))
                {
                    var nonce = ctx.Request.HttpContext.Items["nonce"]?.ToString();
                    ctx.ProtocolMessage.SetParameter("acr_values", "urn:auth:nonce");
                    ctx.ProtocolMessage.SetParameter("login_hint_token", nonce);
                }

                ctx.ProtocolMessage.SetParameter("country_code", "ES");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ...

app.Use((context, next) =>
    {
        if (context.Request.Query.TryGetValue("nonce", out var nonceValue))
        {
            context.Items["nonce"] = nonceValue.ToString();

            var remaining = context.Request.Query
                .Where(kvp => kvp.Key != "nonce")
                .SelectMany(kvp => kvp.Value, (kvp, v) => new KeyValuePair<string, string?>(kvp.Key, v));

            context.Request.QueryString = QueryString.Create(remaining);

            return context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
);

// ...

```

[[go back to index]](../index.md#documentation-structure)
