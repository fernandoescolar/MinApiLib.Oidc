[[go back to index]](../index.md#documentation-structure)

# Defining Clients

Clients represent applications that can request tokens from the server.

The details vary, but you typically define the following common settings for a client:

* a unique client ID
* a secret if needed
* the allowed interactions with the token service (called a grant type)
* a network location where identity and/or access token gets sent to (called a redirect URI)
* a list of scopes (aka resources) the client is allowed to access

#### NOTE
At runtime, clients are retrieved via an implementation of `IClientStore`. This allows loading them from arbitrary
data sources like configuration files or databases. This page uses the in-memory client store, registered with the
`AddInMemoryClients` builder method. `AddMongoStores` from `MinApiLib.Oidc.Server.MongoDB` keeps clients in MongoDB
instead (see [Startup](startup.md#refstartup)). `Client` lives in `MinApiLib.Oidc.Server.Core.Models`; see the
[client reference](../reference/client.md) for all its settings.

## Defining a client for server to server communication

In this scenario no interactive user is present - a service (aka client) wants to communicate with an API (aka scope):

```csharp
public static class Clients
{
    public static IEnumerable<Client> Get() =>
    [
        new Client
        {
            ClientId = "service.client",
            ClientSecrets = { new Secret("secret".Sha256()) },

            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api1", "api2.read_only" }
        }
    ];
}
```

Set `RequireDPoP = true` to make the client prove possession of a key on every token request; its access tokens are
then bound to that key (see [Proof-of-Possession Access Tokens](pop.md)).

<a id="startclientsmvc"></a>

## Defining an interactive application for use authentication and delegated API access

Interactive applications (e.g. web applications or native desktop/mobile applications) use the authorization code
flow with PKCE. This flow gives you the best security because the access tokens are transmitted via back-channel
calls only (and gives you access to refresh tokens). `RequirePkce` is `true` by default:

```csharp
var interactiveClient = new Client
{
    ClientId = "interactive",

    AllowedGrantTypes = GrantTypes.Code,
    AllowOfflineAccess = true,
    ClientSecrets = { new Secret("secret".Sha256()) },

    RedirectUris =           { "https://web.example.com/signin-oidc" },
    PostLogoutRedirectUris = { "https://web.example.com/signout-callback-oidc" },
    FrontChannelLogoutUri =    "https://web.example.com/signout-oidc",

    AllowedScopes =
    {
        OidcServerConstants.StandardScopes.OpenId,
        OidcServerConstants.StandardScopes.Profile,
        OidcServerConstants.StandardScopes.Email,

        "api1", "api2.read_only"
    },
};
```

#### NOTE
See the [grant types](grant_types.md#refgranttypes) topic for more information on choosing the right grant type
for your client. The [sample server's Config.cs](../../samples/Server/Config.cs) defines a web client and a worker,
each with bearer and DPoP-bound tokens.

## Defining clients in appsettings.json

The `AddInMemoryClients` method also accepts a section of the ASP.NET Core configuration. This allows you to
define static clients in `appsettings.json`:

```json
"OidcServer": {
  "IssuerUri": "https://login.example.com",
  "Clients": [
    {
      "Enabled": true,
      "ClientId": "local-dev",
      "ClientName": "Local Development",
      "ClientSecrets": [ { "Value": "<Insert Sha256 hash of the secret encoded as Base64 string>" } ],
      "AllowedGrantTypes": [ "client_credentials" ],
      "AllowedScopes": [ "api1" ]
    }
  ]
}
```

Then pass the configuration section to the `AddInMemoryClients` method:

```csharp
builder.Services.AddOidcServer(builder.Configuration.GetSection("OidcServer"))
    .AddInMemoryClients(builder.Configuration.GetSection("OidcServer:Clients"));
```

[[go back to index]](../index.md#documentation-structure)
