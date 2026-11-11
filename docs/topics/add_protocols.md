[[go back to index]](../index.md#documentation-structure)

# Adding new Protocols

The server lets the host add other protocols besides the built-in support for OpenID Connect and OAuth 2.0, e.g.
WS-Federation or SAML.

You add those protocol endpoints as MinApiLib endpoint records in the host's assembly, mapped with
`app.MapEndpoints()` (or as any other ASP.NET Core endpoint or middleware). They have access to the dependency
injection container, so they can reuse the server's services, like the client definitions or the key material. A
record that calls `AsOidcServerEndpoint` from its `Configure` gets the same events for unhandled exceptions and
logging as the protocol endpoints (see "Custom endpoints" in the [server README](../../src/MinApiLib.Oidc.Server/README.md)):

```csharp
public sealed record WsFederationEndpoint() : GetHandlerAsync<WsFederationEndpoint.Request>("/wsfed")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IClientStore Clients,
        [FromServices] IKeyMaterialService Keys,
        [FromServices] IOidcServerInteractionService Interaction);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.AsOidcServerEndpoint("WsFederation", Path, GetType());

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // validate the request, send the user to the login page, create the response
        return Results.Empty;
    }
}
```

## Typical authentication workflow

An authentication request typically works like this:

* the authentication request arrives at the protocol endpoint
* the protocol endpoint validates its input
* if the user is anonymous, it redirects to the login page with a return URL back to the protocol endpoint
  * the login page reads the current request details through the `IOidcServerInteractionService`
  * it authenticates the user (either locally or via external authentication handlers)
  * it signs the user in
  * it redirects back to the protocol endpoint
* the protocol endpoint creates the protocol response (token creation and redirect back to the client)

## Useful server services

The workflow above needs some interaction points with the server.

**Access to configuration and redirecting to the login page**

Inject `OidcServerOptions` to read the server's configuration. It has, e.g., the path of the login page:

```csharp
var returnUrl = QueryHelpers.AddQueryString("/wsfed", request.HttpContext.Request.Query);

var url = QueryHelpers.AddQueryString(
    options.UserInteraction.LoginUrl,
    options.UserInteraction.LoginReturnUrlParameter,
    returnUrl);

return Results.Redirect(url);
```

(`QueryHelpers` is in `Microsoft.AspNetCore.WebUtilities`. `LoginUrl` and `LoginReturnUrlParameter` default to
`/account/login` and `returnUrl`.)

**Interaction between the login page and the current protocol request**

`IOidcServerInteractionService` turns a protocol return URL into a parsed and validated context object:

```csharp
var context = await interaction.GetAuthorizationContextAsync(returnUrl);
```

By default the interaction service only understands OpenID Connect protocol messages. To extend it, write your own
`IReturnUrlParser` (namespace `MinApiLib.Oidc.Server.Features.Interaction`):

```csharp
public interface IReturnUrlParser
{
    Task<AuthorizationRequest?> ParseAsync(string returnUrl);

    bool IsValidReturnUrl(string returnUrl);
}
```

...and register it in the service collection. It is added next to the OpenID Connect parser, whether you register it
before or after `AddOidcServer()`:

```csharp
builder.Services.AddOidcServer();
builder.Services.AddTransient<IReturnUrlParser, WsFederationReturnUrlParser>();
```

This lets the login page get information like the client configuration and other protocol parameters.

**Access to configuration and key material for creating the protocol response**

`IKeyMaterialService` (namespace `MinApiLib.Oidc.Server.Core.Keys`) gives access to the configured signing
credentials and validation keys:

```csharp
var credentials = await keys.GetSigningCredentialsAsync();

var descriptor = new SecurityTokenDescriptor
{
    Issuer = request.HttpContext.GetOidcServerIssuerUri(),
    Audience = client.ClientId,
    Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime),
    SigningCredentials = credentials,
    Subject = new ClaimsIdentity(outgoingClaims)
};
```

`GetOidcServerIssuerUri()` is an `HttpContext` extension in `MinApiLib.Oidc.Server.Core.Extensions`. To issue a JWT
with the server's token creation engine instead, use [OidcServerTools](tools.md).

[[go back to index]](../index.md#documentation-structure)
