[[go back to index]](../index.md#documentation-structure)

# Adding more API Endpoints

It's a common scenario to add API endpoints to the application that hosts the server, protected by access tokens
the server itself issues.

The server comes with a helper for that, the local API authentication handler (namespace
`MinApiLib.Oidc.Server.Features.LocalApi`). It validates tokens with the server's own token validation, in process, so
it needs no discovery document or HTTP calls, and it accepts reference tokens as well as JWTs. See the advanced
section to understand more of the internal plumbing.

#### NOTE
You could achieve the same with Microsoft's `JwtBearer` handler pointed at the server itself, but that needs more
configuration and fetches the server's own discovery document over HTTP.

Start by registering your API as an `ApiScope` (or an `ApiResource` with that scope), e.g.:

```csharp
public static IEnumerable<ApiScope> ApiScopes =>
[
    // local API
    new ApiScope(OidcServerConstants.LocalApi.ScopeName)
];
```

...and give your clients access to it, e.g.:

```csharp
new Client
{
    // rest omitted
    AllowedScopes = { OidcServerConstants.LocalApi.ScopeName },
}
```

#### NOTE
The value of `OidcServerConstants.LocalApi.ScopeName` is `OidcServerApi` (it was `IdentityServerApi` in
IdentityServer).

To enable token validation for local APIs, add the following to the server's startup:

```csharp
builder.Services.AddLocalApiAuthentication();
```

The endpoints themselves are MinApiLib endpoint records in the host's assembly, mapped with `app.MapEndpoints()`
next to `app.MapOidcServer()`. To protect one, require the `OidcServerConstants.LocalApi.PolicyName` policy:

```csharp
public sealed record UsersEndpoint() : GetHandler<UsersEndpoint.Request>("/localapi/users")
{
    public readonly record struct Request(HttpContext HttpContext);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.RequireAuthorization(OidcServerConstants.LocalApi.PolicyName);

    protected override IResult Handle(Request request)
        => Results.Ok(new { client = request.HttpContext.User.FindFirst("client_id")?.Value });
}
```

```csharp
var app = builder.Build();

app.UseRouting();
app.UseOidcServer();
app.UseAuthorization();

app.MapOidcServer();
app.MapEndpoints();   // the host's own endpoint records, UsersEndpoint among them

app.Run();
```

Authorized clients can then request a token for the `OidcServerApi` scope and use it to call the API:

```http
GET /localapi/users HTTP/1.1
Host: login.example.com
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjhDNUM0RjJB…
```

The handler also accepts DPoP-bound tokens, sent as `Authorization: DPoP <token>` with a `DPoP` proof, and rejects
them when they are sent as bearer tokens (see [proof of possession](pop.md#dpop)). A request it rejects gets `401`.

A custom endpoint that should behave like a protocol endpoint (events for unhandled exceptions, logging, left out of
the API description) calls `AsOidcServerEndpoint(name, Path, GetType())` from its `Configure` (see "Custom endpoints"
in the [server README](../../src/MinApiLib.Oidc.Server/README.md)).

## Discovery

You can also add your endpoints to the discovery document if you want, e.g. like this:

```csharp
builder.Services.AddOidcServer(options =>
{
    options.Discovery.CustomEntries.Add("local_api", "~/localapi");
});
```

## Advanced

Under the covers, the `AddLocalApiAuthentication` helper does a couple of things:

* adds an authentication handler that validates incoming tokens with the server's built-in token validation. Its
  scheme name is `OidcServerAccessToken` (`OidcServerConstants.LocalApi.AuthenticationScheme`);
* configures the handler to require a scope claim of value `OidcServerApi` in the access token
  (`LocalApiAuthenticationOptions.ExpectedScope`);
* sets up an authorization policy, `OidcServerConstants.LocalApi.PolicyName`, that authenticates with that scheme and
  requires an authenticated user.

This covers the most common scenarios. You can customize this behavior in the following ways:

* Add the authentication handler yourself by calling `builder.Services.AddAuthentication().AddLocalApi(...)`:
  * this way you can specify the required scope name yourself, or (by specifying no scope at all) accept any token
    from the server;
  * `SaveToken` (on by default) stores the access token in the authentication properties.
* Do your own scope validation/authorization in your endpoints using custom policies or code, e.g.:
  ```csharp
  builder.Services.AddAuthorization(options =>
  {
      options.AddPolicy(OidcServerConstants.LocalApi.PolicyName, policy =>
      {
          policy.AddAuthenticationSchemes(OidcServerConstants.LocalApi.AuthenticationScheme);
          policy.RequireAuthenticatedUser();
          // custom requirements
      });
  });
  ```

## Claims Transformation

You can provide a callback to transform the claims of the incoming token after validation.
Either use the helper method, e.g.:

```csharp
builder.Services.AddLocalApiAuthentication(principal =>
{
    principal.Identities.First().AddClaim(new Claim("additional_claim", "additional_value"));

    return Task.FromResult(principal);
});
```

...or set `LocalApiAuthenticationOptions.Events` (`LocalApiAuthenticationEvents.OnClaimsTransformation`) if you add
the authentication handler yourself.

[[go back to index]](../index.md#documentation-structure)
