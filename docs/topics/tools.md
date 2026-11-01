[[go back to index]](../index.md#documentation-structure)

# Tools

The `OidcServerTools` class (namespace `MinApiLib.Oidc.Server.Core.TokenServices`) gives extensibility code access to
the server's token creation engine. `AddOidcServer()` registers it; inject it where you need it, e.g. in a service:

```csharp
public sealed class BackendApiClient(OidcServerTools tools, HttpClient http)
{
    // ...
}
```

Its `IssueJwtAsync` methods create a JWT, signed with the server's signing key, from a lifetime in seconds and a set
of claims:

* `IssueJwtAsync(lifetime, claims)` uses the issuer name of the current request, or `OidcServerOptions.IssuerUri`
  when it is set (outside an HTTP request it must be set);
* `IssueJwtAsync(lifetime, issuer, claims)` uses the issuer you pass;
* `IssueJwtAsync(lifetime, issuer, tokenType, claims)` also sets the token type (see
  `OidcServerConstants.TokenTypes`), which drives the JWT `typ` header: `at+jwt` for access tokens, `logout+jwt`
  for logout tokens.

The `IssueClientJwtAsync` extension method (in `OidcServerToolsExtensions`) is an easier version for server-to-server
communication, e.g. when your code has to call an API protected by the server. It issues an access token with the
`client_id` claim, the scopes, the audiences and any additional claims you pass:

```csharp
var token = await tools.IssueClientJwtAsync(
    clientId: "client_id",
    lifetime: 3600,
    scopes: ["backend.read"],
    audiences: ["backend.api"]);

http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

With `OidcServerOptions.EmitStaticAudienceClaim`, the token also gets the `{issuer}/resources` audience.

The tokens are not tied to a registered client or to a token request: no client configuration, resource validation or
profile service is involved, and no event is raised.

[[go back to index]](../index.md#documentation-structure)
