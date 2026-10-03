# MinApiLib.Oidc.Server.MongoDB

MongoDB stores for [MinApiLib.Oidc.Server](../MinApiLib.Oidc.Server/README.md). One call keeps all of this in
MongoDB:

- **Configuration:** clients, identity resources, API resources, API scopes, and the CORS origins of the clients.
- **Operational data:** persisted grants (authorization codes, refresh and reference tokens, consents, pushed
  authorization requests, nonces) and device codes.
- **Signing keys** of automatic key management, shared by every server instance that uses the database.

It uses the official [MongoDB C# driver](https://www.mongodb.com/docs/drivers/csharp/) (Apache-2.0) and targets
`net10.0` and `net11.0`.

## Usage

```csharp
builder.Services.AddOidcServer(options => options.KeyManagement.Enabled = true)
    .AddMongoStores(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Mongo");
        options.DatabaseName = "login";
    });
```

| Option | Default | |
|---|---|---|
| `ConnectionString` | | Leave it empty to use an `IMongoClient` registered by the host |
| `DatabaseName` | `oidcserver` | |
| `CollectionPrefix` | empty | For databases shared with other applications |
| `UseForConfiguration` | `true` | Clients, resources and CORS origins |
| `UseForOperationalData` | `true` | Persisted grants and device codes |
| `UseForSigningKeys` | `true` | Keys of automatic key management (`KeyManagement.Enabled`) |
| `CreateIndexes` | `true` | Creates the indexes when the host starts |

Turn off a group to keep it elsewhere: for example, `UseForConfiguration = false` with `AddInMemoryClients(...)`
keeps the clients in code and only the grants in MongoDB. The registered stores are the usual ones, so
`AddClientStoreCache<MongoClientStore>()` and the other caching helpers work on top of them.

## Collections

| Collection | Document id | Contents |
|---|---|---|
| `clients` | `ClientId` | `Client`, as is |
| `identityResources`, `apiResources`, `apiScopes` | `Name` | `IdentityResource`, `ApiResource`, `ApiScope`, as is |
| `persistedGrants` | grant key | `PersistedGrantDocument`: the grant's metadata and its serialized data |
| `deviceCodes` | device code | `DeviceFlowDocument`: user code, subject, client, expiration and the serialized `DeviceCode` |
| `signingKeys` | key id | `SigningKeyDocument`: the serialized key, protected with ASP.NET Core Data Protection by default |

Enums are stored as strings, and unknown fields are ignored, so documents written by a newer version still load.

`persistedGrants` and `deviceCodes` have a TTL index on `Expiration`: MongoDB deletes expired documents, so the
server needs no clean-up job. MongoDB checks TTL indexes about once a minute; until then the server rejects the
expired entries itself.

## Managing the configuration

`OidcServerMongoContext` exposes the collections and upserts, for seeding or for an administration tool:

```csharp
var context = app.Services.GetRequiredService<OidcServerMongoContext>();

await context.UpsertAsync(new Client
{
    ClientId = "web",
    ClientSecrets = { new Secret("secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.Code,
    RedirectUris = { "https://web.example.com/signin-oidc" },
    AllowedScopes = { "openid", "profile", "orders.read" }
});
await context.UpsertAsync(new ApiScope("orders.read"));
await context.UpsertAsync(new ApiResource("orders") { Scopes = { "orders.read" } });
```

The [sample server](../../samples/README.md) seeds its configuration this way when it runs with MongoDB.

## Tests

`tests/MinApiLib.Oidc.Server.MongoDB.Tests` runs the stores and a server end to end against a real MongoDB, which
[Testcontainers](https://dotnet.testcontainers.org/) starts in Docker (`mongo:8.0`). Running them needs Docker.

## License

Apache License 2.0, like MinApiLib.Oidc.Server. The repository's [NOTICE](../../NOTICE) holds the attribution.
