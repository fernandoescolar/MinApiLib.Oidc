[[go back to index]](../index.md#documentation-structure)

# Adding a JavaScript client

#### NOTE
For the prerequisites, see the [overview](0_overview.md) first.

This quickstart builds a browser-based JavaScript application, a "single-page application" (SPA). The user signs in
to the server, the application calls the API with the access token the server issued, and the user signs out. The
JavaScript code in the browser does all of it.

It uses the server from quickstart 1 (https://localhost:5001) and its API (https://localhost:5003, which requires the
`orders.read` scope). The SPA runs on https://localhost:5004.

## Should the browser hold the tokens?

A SPA is a *public client*: it cannot keep a secret, and the tokens live in the browser, where any script that runs
on the page (an XSS flaw, a compromised dependency) can read them. For most applications a **backend for frontend**
(BFF) is the better design: a small server-side application signs the user in as a confidential client, keeps the
tokens on the server, gives the browser only a session cookie, and forwards the API calls. That is what the
[web client sample](../../samples/WebClient) does with [MinApiLib.Oidc.Authentication](../../src/MinApiLib.Oidc.Authentication/README.md),
and [quickstart 3](3_aspnetcore_and_apis.md) builds such a server-side client.

When the tokens must live in the browser, use the authorization code flow with PKCE, as below: the implicit flow is
off by default and should stay off. Short token lifetimes help, and so does binding the tokens to a key with
[DPoP](../topics/pop.md).

## The protocol

The SPA talks to the server like any other client, with two differences: it has no secret, and the browser calls the
token endpoint across origins, so the server must allow the SPA's origin (CORS).

1. The SPA creates a PKCE code verifier, keeps it in session storage and sends the browser to the authorize endpoint
   with its hash:

   ```http
   GET /connect/authorize?client_id=spa
       &response_type=code
       &scope=openid%20profile%20orders.read
       &redirect_uri=https%3A%2F%2Flocalhost%3A5004%2Fcallback.html
       &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
       &code_challenge_method=S256
       &state=af0ifjsldkj HTTP/1.1
   Host: localhost:5001
   ```

2. The user signs in, and the server redirects back to `callback.html` with the code. The SPA redeems it with `fetch`,
   without a secret and with the verifier:

   ```http
   POST /connect/token HTTP/1.1
   Host: localhost:5001
   Origin: https://localhost:5004
   Content-Type: application/x-www-form-urlencoded

   grant_type=authorization_code
   &client_id=spa
   &code=SplxlOBeZQQYbYS6WxSbIA
   &redirect_uri=https%3A%2F%2Flocalhost%3A5004%2Fcallback.html
   &code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
   ```

   ```http
   HTTP/1.1 200 OK
   Access-Control-Allow-Origin: https://localhost:5004
   Content-Type: application/json

   { "id_token": "eyJ...", "access_token": "eyJ...", "expires_in": 3600, "token_type": "Bearer", "scope": "openid profile orders.read" }
   ```

   The server answers the CORS headers because the client lists the origin in `AllowedCorsOrigins`.

3. The SPA calls the API with the access token, and signs out through the end session endpoint with the identity
   token as `id_token_hint`.

A library does this for you. This quickstart uses [oidc-client-ts](https://github.com/authts/oidc-client-ts), a
third-party library (Apache-2.0, not part of MinApiLib.Oidc) and the maintained successor of `oidc-client`. Any
OpenID Connect library for the browser that supports the code flow with PKCE works the same way.

## Register the client at the server

In the server from quickstart 1, add the SPA to the clients:

```csharp
new Client
{
    ClientId = "spa",
    ClientName = "JavaScript client",

    AllowedGrantTypes = GrantTypes.Code,
    RequireClientSecret = false,    // a public client. RequirePkce is on by default

    RedirectUris = { "https://localhost:5004/callback.html" },
    PostLogoutRedirectUris = { "https://localhost:5004/" },
    AllowedCorsOrigins = { "https://localhost:5004" },

    AllowedScopes =
    {
        OidcServerConstants.StandardScopes.OpenId,
        OidcServerConstants.StandardScopes.Profile,
        "orders.read"
    }
}
```

`AllowedCorsOrigins` lets the browser call the discovery, token, userinfo and revocation endpoints from that origin.
The default CORS policy service of the in-memory clients (and of the MongoDB stores) reads it from the clients.

For refresh tokens, add `AllowOfflineAccess = true` and the `offline_access` scope. Refresh tokens are one-time-only
by default (`RefreshTokenUsage`), so each refresh returns a new one.

## Create the JavaScript client

Create an empty ASP.NET Core application to serve the static files:

```bash
dotnet new web -o JavaScriptClient
```

Make it listen on https://localhost:5004 (`applicationUrl` in `Properties/launchSettings.json`) and replace
`Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();   // / serves index.html
app.UseStaticFiles();    // from wwwroot

app.Run();
```

Any static file host works as well; nothing here needs ASP.NET Core.

Install oidc-client-ts and a bundler (esbuild here) with npm, in the project folder:

```bash
npm install oidc-client-ts
npm install --save-dev esbuild
```

### wwwroot/index.html

Buttons to sign in, call the API and sign out, and a `<pre>` for the results:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>JavaScript client</title>
</head>
<body>
    <button id="login">Sign in</button>
    <button id="api">Call API</button>
    <button id="logout">Sign out</button>

    <pre id="results"></pre>

    <script type="module" src="app.js"></script>
</body>
</html>
```

### wwwroot/callback.html

The `redirect_uri`. It loads the same script, which finishes the sign-in:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
</head>
<body>
    <script type="module" src="app.js"></script>
</body>
</html>
```

### app.js

In the project folder, not in `wwwroot`: the bundler writes `wwwroot/app.js` from it. `UserManager` runs the protocol. On the callback page, `signinRedirectCallback()` checks the `state`, redeems the
code at the token endpoint and keeps the user, with its tokens, in session storage. `getUser()` reads it back,
`signinRedirect()` and `signoutRedirect()` start the sign-in and the sign-out:

```javascript
import { UserManager } from "oidc-client-ts";

const userManager = new UserManager({
    authority: "https://localhost:5001",
    client_id: "spa",
    redirect_uri: "https://localhost:5004/callback.html",
    post_logout_redirect_uri: "https://localhost:5004/",
    response_type: "code",
    scope: "openid profile orders.read"
});

function log(...messages) {
    document.getElementById("results").textContent = messages
        .map(m => typeof m === "string" ? m : JSON.stringify(m, null, 2))
        .join("\n");
}

if (location.pathname === "/callback.html") {
    await userManager.signinRedirectCallback();
    location.replace("/");
} else {
    const user = await userManager.getUser();
    log(user ? "Signed in" : "Not signed in", user?.profile ?? "");

    document.getElementById("login").onclick = () => userManager.signinRedirect();
    document.getElementById("logout").onclick = () => userManager.signoutRedirect();
    document.getElementById("api").onclick = async () => {
        const user = await userManager.getUser();
        if (!user) {
            log("Sign in first");
            return;
        }

        const response = await fetch("https://localhost:5003/orders", {
            headers: { Authorization: `Bearer ${user.access_token}` }
        });
        log(response.status, response.ok ? await response.json() : await response.text());
    };
}
```

Bundle it into `wwwroot`:

```bash
npx esbuild app.js --bundle --format=esm --outfile=wwwroot/app.js
```

## Allow the SPA to call the API (CORS)

The API is on another origin, and the `Authorization` header makes the browser send a preflight request first. In
the API's `Program.cs`, allow the SPA's origin:

```csharp
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("https://localhost:5004")
    .WithHeaders("Authorization")
    .WithMethods("GET")));

// ...

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

## Run the JavaScript client

Start the server, the API and the JavaScript client, and open https://localhost:5004:

1. **Sign in** goes to the server's login page and comes back to `callback.html`, then to the home page, which shows
   the claims of the identity token (`user.profile`).
2. **Call API** sends the access token and shows the API's answer.
3. **Sign out** goes to the server's end session endpoint, which signs the user out and offers the way back to the
   SPA.

You now have a JavaScript client that signs in with the server, calls an API with its access token and signs out.

oidc-client-ts can also refresh the tokens before they expire (`automaticSilentRenew`), watch the session at the
server ([session management](../topics/signout.md)) and send DPoP proofs; see its documentation.

[[go back to index]](../index.md#documentation-structure)
