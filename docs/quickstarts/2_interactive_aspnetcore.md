[[go back to index]](../index.md#documentation-structure)

# Interactive Applications with ASP.NET Core

In this quickstart you add interactive user authentication with OpenID Connect to the server of the
[previous quickstart](1_client_credentials.md), and create an ASP.NET Core web application that signs users in with
it.

Do the [preparation](0_overview.md#preparation) and [quickstart 1](1_client_credentials.md) first. The finished code
is in [samples/](../../samples/README.md): `Server` and `WebClient`.

## Adding the UI

The server implements the protocol, but it has no UI: the host writes its own login, logout and error pages (and a
consent page, if clients require consent). There is no ready-made UI package or template. The sample server has
minimal pages; this quickstart builds a smaller version of them.

This is how the server and the login page work together:

1. A client sends the user to the authorize endpoint. The user has no session, so the server redirects to the login
   page, with the authorize request in the return URL:
   `/account/login?returnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Fclient_id%3Dweb...`.
2. The login page reads the request from the return URL with `IOidcServerInteractionService.GetAuthorizationContextAsync`:
   the client, `login_hint`, `acr_values`, the requested scopes and so on.
3. Once it has checked the user's credentials, the page signs the user in to the server's cookie with
   `HttpContext.SignInAsync(OidcServerUser)`, and redirects back to the return URL.
4. The authorize endpoint finds the session and answers the client.

By default the server sends the user to `/account/login` with a `returnUrl` parameter, to `/account/logout` with a
`logoutId` parameter, and to `/home/error` with an `errorId` parameter. `options.UserInteraction` changes them
(`LoginUrl`, `LoginReturnUrlParameter`, `LogoutUrl`, `ErrorUrl`, ...); see [Server Options](../reference/options.md).

### The users

The quickstart keeps two users in code, with their passwords in clear text, to keep the focus on the protocol. Add a
`Users.cs` file to the server:

```csharp
public sealed record User(string Id, string UserName, string Password, string Name, string Email);

// Demo only. A real server keeps users in a database with hashed passwords, for example with ASP.NET Core Identity.
public static class Users
{
    private static readonly User[] All =
    [
        new("1", "alice", "alice", "Alice Smith", "alice@example.com"),
        new("2", "bob", "bob", "Bob Jones", "bob@example.com")
    ];

    public static User? Find(string? userName, string? password)
        => All.FirstOrDefault(u => u.UserName == userName && u.Password == password);
}
```

The sample server keeps its users in ASP.NET Core Identity, with hashed passwords and passkeys; see
[Using ASP.NET Core Identity and passkeys](6_aspnet_identity.md).

### The login page

Add the login page to `Server/Program.cs`. It is two endpoints: a `GET` that renders the form and a `POST` that checks
it. The form is protected with an antiforgery token: binding a form with `[FromForm]` turns on antiforgery
validation, so register the services and the middleware too.

```csharp
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MinApiLib.Oidc.Server.Core.Models;
using MinApiLib.Oidc.Server.Features.Interaction;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOidcServer()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddDeveloperSigningCredential();

builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseRouting();
app.UseOidcServer();
app.UseAntiforgery();

app.MapOidcServer();

app.MapGet("/account/login", async (string? returnUrl, HttpContext http,
    IOidcServerInteractionService interaction, IAntiforgery antiforgery) =>
{
    var context = returnUrl == null ? null : await interaction.GetAuthorizationContextAsync(returnUrl);
    return LoginPage(http, antiforgery, returnUrl, context, error: null);
});

app.MapPost("/account/login", async ([FromForm] string? username, [FromForm] string? password,
    [FromForm] string? returnUrl, HttpContext http,
    IOidcServerInteractionService interaction, IAntiforgery antiforgery) =>
{
    var context = returnUrl == null ? null : await interaction.GetAuthorizationContextAsync(returnUrl);
    var user = Users.Find(username, password);
    if (user == null)
    {
        return LoginPage(http, antiforgery, returnUrl, context, "Invalid user name or password.");
    }

    // The server's session: the subject, how the user signed in (amr) and the claims for the tokens.
    await http.SignInAsync(new OidcServerUser(user.Id)
    {
        DisplayName = user.Name,
        AuthenticationMethods = ["pwd"],
        AdditionalClaims = { new Claim("email", user.Email) }
    });

    // Back to the authorize request. Anything else could be an open redirect.
    return context != null ? Results.Redirect(returnUrl!) : Results.Redirect("/");
});

app.Run();

static IResult LoginPage(HttpContext http, IAntiforgery antiforgery, string? returnUrl,
    AuthorizationRequest? context, string? error)
{
    var tokens = antiforgery.GetAndStoreTokens(http);
    var client = context?.Client.ClientName ?? context?.Client.ClientId;
    return Results.Content($"""
        <!doctype html>
        <title>Sign in</title>
        <h1>Sign in{(client == null ? "" : " to " + WebUtility.HtmlEncode(client))}</h1>
        <p>{WebUtility.HtmlEncode(error)}</p>
        <form method="post" action="/account/login">
          <input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">
          <input type="hidden" name="returnUrl" value="{WebUtility.HtmlEncode(returnUrl)}">
          <label>User name <input name="username" autofocus></label>
          <label>Password <input name="password" type="password"></label>
          <button>Sign in</button>
        </form>
        """, "text/html");
}
```

`OidcServerUser` describes the session: `SubjectId` becomes the `sub` claim, `DisplayName` the `name` claim,
`AuthenticationMethods` the `amr` claim ([RFC 8176](https://www.rfc-editor.org/rfc/rfc8176) values, such as `pwd`),
and `AdditionalClaims` go into the session cookie as they are. `SignInAsync(OidcServerUser)` is an extension method
of the server (namespace `Microsoft.AspNetCore.Http`) that signs in to the server's cookie scheme.

The page only redirects to the return URL when it is a valid authorize request (`context != null`). You can also
check it with `interaction.IsValidReturnUrl(returnUrl)`.

The sample server's [LoginPage.cs](../../samples/Server/Pages/LoginPage.cs) and [Login.cs](../../samples/Server/Login.cs)
do the same with MinApiLib endpoint records, ASP.NET Core Identity, passkeys and `acr_values`.

### The logout and error pages

The end session endpoint redirects to the logout page with a `logoutId`. The page reads the logout request with
`GetLogoutContextAsync`, signs the user out of the server, and loads the front-channel logout iframe, which signs the
user out of the clients of the session. Then it offers the way back to the client:

```csharp
app.MapGet("/account/logout", async (string? logoutId, HttpContext http, IOidcServerInteractionService interaction) =>
{
    var logout = await interaction.GetLogoutContextAsync(logoutId ?? string.Empty);
    await http.SignOutAsync();

    var back = logout.PostLogoutRedirectUri == null ? "" :
        $"""<p><a href="{WebUtility.HtmlEncode(logout.PostLogoutRedirectUri)}">Return to {WebUtility.HtmlEncode(logout.ClientName ?? logout.ClientId)}</a></p>""";
    var iframe = logout.SignOutIFrameUrl == null ? "" :
        $"""<iframe src="{WebUtility.HtmlEncode(logout.SignOutIFrameUrl)}" hidden></iframe>""";
    return Results.Content($"<!doctype html><title>Signed out</title><h1>You are signed out</h1>{back}{iframe}", "text/html");
});
```

This page signs out on a `GET`, like the sample. `logout.ShowSignoutPrompt` is `true` when the request did not come
with a valid `id_token_hint`, that is, when the server cannot tell that the client sent it: a real logout page should
then ask the user to confirm. See [Sign-out](../topics/signout.md).

The error page shows the errors the server cannot send back to the client, such as an unknown redirect URI:

```csharp
app.MapGet("/home/error", async (string? errorId, IOidcServerInteractionService interaction) =>
{
    var error = errorId == null ? null : await interaction.GetErrorContextAsync(errorId);
    return Results.Content($"""
        <!doctype html><title>Error</title><h1>Error</h1>
        <p>{WebUtility.HtmlEncode(error?.Error)}: {WebUtility.HtmlEncode(error?.ErrorDescription)}</p>
        """, "text/html");
});
```

## Adding support for OpenID Connect identity scopes

Like OAuth 2.0, OpenID Connect uses scopes. Here they do not represent APIs but identity data: the user id
(`openid`), the profile (`profile`: name, ...) and the email address (`email`). Add them to `Config`:

```csharp
public static IEnumerable<IdentityResource> IdentityResources =>
[
    new IdentityResources.OpenId(),
    new IdentityResources.Profile(),
    new IdentityResources.Email()
];
```

and register them:

```csharp
builder.Services.AddOidcServer()
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddDeveloperSigningCredential();
```

The OpenID Connect [specification](https://openid.net/specs/openid-connect-core-1_0.html#ScopeClaims) lists the
standard scopes and their claims.

## Adding the web client to the server configuration

An interactive client is like the client of quickstart 1, but it uses the authorization code flow, so it needs the
addresses the server may redirect to. Add it to `Config.Clients`:

```csharp
new Client
{
    ClientId = "web",
    ClientSecrets = { new Secret("web-secret".Sha256()) },

    // authorization code flow with PKCE
    AllowedGrantTypes = GrantTypes.Code,
    RequirePkce = true,

    // this quickstart has no consent page
    RequireConsent = false,

    // put the profile claims in the identity token, so the client does not call the userinfo endpoint
    AlwaysIncludeUserClaimsInIdToken = true,

    // where to redirect after login, after logout, and the front-channel logout page of the client
    RedirectUris = { "https://localhost:5002/signin-oidc" },
    PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },
    FrontChannelLogoutUri = "https://localhost:5002/signout-oidc",

    AllowedScopes = { "openid", "profile", "email" }
}
```

The paths are the defaults of the ASP.NET Core OpenID Connect handler.

## Creating the web client

Create the web application:

```bash
dotnet new web -n WebClient
dotnet sln add WebClient
dotnet add WebClient package MinApiLib.Oidc.Authentication --prerelease
```

`MinApiLib.Oidc.Authentication` brings the ASP.NET Core OpenID Connect handler
(`Microsoft.AspNetCore.Authentication.OpenIdConnect`) in the version that matches your framework. You use the
package's own features in the [next quickstart](3_aspnetcore_and_apis.md).

Set its address to `https://localhost:5002` in `WebClient/Properties/launchSettings.json`, and replace
`WebClient/Program.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("cookie")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:5001";
        options.ClientId = "web";
        options.ClientSecret = "web-secret";
        options.ResponseType = "code";
        options.UsePkce = true;

        // "openid" and "profile" are in the list by default
        options.Scope.Add("email");

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (ClaimsPrincipal user) => new
{
    name = user.Identity!.Name,
    claims = user.Claims.Select(c => new { c.Type, c.Value })
}).RequireAuthorization();

app.MapGet("/logout", () => Results.SignOut(
    new AuthenticationProperties { RedirectUri = "/signed-out" }, ["cookie", "oidc"]));

app.MapGet("/signed-out", () => "Signed out.");

app.Run();
```

* `AddAuthentication` sets the cookie as the default scheme: the application keeps its own session in a cookie. The
  challenge scheme is `oidc`: when a user must sign in, the OpenID Connect handler sends them to the server.
* `AddOpenIdConnect` configures the handler: where the server is (`Authority`), the client id and secret, and the
  authorization code flow (`ResponseType = "code"`) with PKCE. `SaveTokens` keeps the tokens in the cookie, for the
  next quickstart.
* `MapInboundClaims = false` keeps the claim names of the token (`sub`, `name`, `email`) instead of mapping them to
  long URIs.
* `RequireAuthorization()` on `/` makes anonymous users sign in.

See [Grant Types](../topics/grant_types.md) for the protocol flows.

## Testing the client

Run the server and the web client, and open `https://localhost:5002`. The page requires a signed-in user, so the
OpenID Connect handler sends you to the server.

The ASP.NET Core OpenID Connect handler pushes the authorize request to the server's
[pushed authorization endpoint](../topics/par.md) first, because the discovery document announces it, and sends the
browser to the authorize endpoint with a reference to it:

```http
GET /connect/authorize?client_id=web&request_uri=urn%3Aietf%3Aparams%3Aoauth%3Arequest_uri%3A74A0BB1E140F61ED... HTTP/1.1
Host: localhost:5001
```

The server redirects to your login page, with the authorize request in `returnUrl`:

```http
HTTP/1.1 302 Found
Location: https://localhost:5001/account/login?returnUrl=%2Fconnect%2Fauthorize%2Fcallback%3Fclient_id%3Dweb%26request_uri%3D...
```

Sign in as `alice` / `alice`. The login page redirects back to the authorize request, and the server posts the
authorization code to the client (`response_mode=form_post`, the handler's default). The handler redeems the code at
the token endpoint with its secret and the PKCE `code_verifier`, validates the identity token and signs the user in to
the application's cookie. The page shows the user's claims:

```json
{
  "name": "Alice Smith",
  "claims": [
    { "type": "sid", "value": "AB971B8965E0F136AED664FEA98E15EB" },
    { "type": "sub", "value": "1" },
    { "type": "auth_time", "value": "1794912843" },
    { "type": "idp", "value": "local" },
    { "type": "name", "value": "Alice Smith" },
    { "type": "email", "value": "alice@example.com" },
    { "type": "amr", "value": "pwd" },
    ...
  ]
}
```

`name` and `email` come from the session the login page created: the server's default profile service
(`DefaultProfileService`) issues the claims of the session that the requested scopes ask for. A real server
implements `IProfileService` over its user store and registers it with `AddProfileService<T>()`; see
[Profile Service](../reference/profileservice.md).

The claims are in the identity token because the client has `AlwaysIncludeUserClaimsInIdToken`. Without it, set
`GetClaimsFromUserInfoEndpoint = true` in the handler, which then gets them from the userinfo endpoint.

## Adding sign-out

Signing out of the application is not enough: the user still has a session on the server, and would be signed in
again without seeing the login page. `/logout` signs out of both schemes: the cookie, and `oidc`, which sends the user
to the server's end session endpoint with the identity token as `id_token_hint`.

The server redirects to your logout page, which ends the server session, loads the front-channel logout iframe
(which calls the `FrontChannelLogoutUri` of the clients of the session), and links back to
`https://localhost:5002/signout-callback-oidc`. The handler then redirects to `/signed-out`.

## Further experiments

Add more claims to the users and more identity resources:

* add an identity resource to `Config.IdentityResources`, with a name and the claims it carries;
* allow it in the client's `AllowedScopes`;
* ask for it with `options.Scope.Add(...)` in the web client;
* put the claims in the session, in `AdditionalClaims`, or return them from your `IProfileService`.

Try the error paths too: a wrong password, a redirect URI that is not registered (the server shows your error page),
or a scope the client is not allowed to ask for.

<a id="refexternalauthenticationquickstart"></a>

## Adding support for external authentication

The login page can also offer external identity providers, such as Google, Microsoft or another OpenID Connect
server. All you need is an ASP.NET Core authentication handler for the provider; ASP.NET Core has handlers for
Google, Facebook, Twitter, Microsoft accounts and OpenID Connect, and
[AspNet.Security.OAuth.Providers](https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers) has many more.

The server registers a cookie scheme for the result of an external sign-in,
`OidcServerConstants.ExternalCookieAuthenticationScheme` (`idsrv.external`). Register the provider's handler with
that scheme as its sign-in scheme. For example, with the `Microsoft.AspNetCore.Authentication.Google` package (in the
version of your framework) and a client id and secret from the Google developer console, with
`https://localhost:5001/signin-google` as the callback address:

```csharp
builder.Services.AddAuthentication()
    .AddGoogle("Google", options =>
    {
        options.SignInScheme = OidcServerConstants.ExternalCookieAuthenticationScheme;
        options.ClientId = "<client id>";
        options.ClientSecret = "<client secret>";
    });
```

Then add a link on the login page, `/account/external?scheme=Google&returnUrl=...`, and two endpoints: one that
starts the external sign-in, and one that receives its result, signs the user in to the server and cleans up the
external cookie:

```csharp
app.MapGet("/account/external", (string scheme, string? returnUrl) =>
    Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = "/account/external/callback",
        Items = { ["scheme"] = scheme, ["returnUrl"] = returnUrl }
    }, [scheme]));

app.MapGet("/account/external/callback", async (HttpContext http, IOidcServerInteractionService interaction) =>
{
    var result = await http.AuthenticateAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);
    if (!result.Succeeded)
    {
        return Results.Redirect("/account/login");
    }

    var provider = result.Properties.Items["scheme"]!;
    var externalId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Find the local user of (provider, externalId), or create one. The sketch uses the external id as is.
    await http.SignInAsync(new OidcServerUser($"{provider}:{externalId}")
    {
        DisplayName = result.Principal.FindFirstValue(ClaimTypes.Name),
        IdentityProvider = provider
    });
    await http.SignOutAsync(OidcServerConstants.ExternalCookieAuthenticationScheme);

    var returnUrl = result.Properties.Items["returnUrl"];
    var context = returnUrl == null ? null : await interaction.GetAuthorizationContextAsync(returnUrl);
    return Results.Redirect(context != null ? returnUrl! : "/");
});
```

`OidcServerConstants` is in the `MinApiLib.Oidc.Server` namespace. What to do with a user who signs in for the first
time is up to you: create a local account, link it to an existing one, or show a registration page first. See
[Sign-in with External Identity Providers](../topics/signin_external_providers.md).

[[go back to index]](../index.md#documentation-structure)
