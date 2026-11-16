[[go back to index]](../index.md#documentation-structure)

# Using MongoDB for configuration and operational data

In the previous quickstarts the clients, scopes and resources are defined in code, and the server loads them into
memory at startup. Changing them means changing the code and restarting the server.

The server also creates data while it runs: authorization codes, refresh tokens, consents, pushed authorization
requests, device codes and, with automatic key management, signing keys. By default the grants live in memory too,
so a restart loses them, and several instances of the server cannot share them.

[MinApiLib.Oidc.Server.MongoDB](../../src/MinApiLib.Oidc.Server.MongoDB/README.md) keeps all of that in MongoDB. The
[sample server](../../samples/README.md#with-mongodb) runs this way with its `https-mongo` profile; this quickstart
follows it.

## Start MongoDB

Any MongoDB works. For development, run it in Docker, with its data in a volume:

```bash
docker run -d --name mongo -p 27017:27017 -v mongo-data:/data/db mongo:8.0
```

The samples' [apphost.cs](../../samples/apphost.cs) does the same with [Aspire](https://aspire.dev), and hands the
connection string to the server:

```csharp
var password = builder.AddParameter("mongodb-password", "minapilib-oidc-samples", secret: true);
var database = builder.AddMongoDB("mongodb", password: password)
    .WithDataVolume("minapilib-oidc-samples-mongodb")
    .AddDatabase("oidcserver-sample");

builder.AddProject<Projects.Server>("server")
    .WithReference(database, connectionName: "Mongo")
    .WaitFor(database);
```

MongoDB creates its admin user only when the volume is empty. Keep the password stable, as here, or keep it with the
volume: if Aspire generates a new one, the server cannot authenticate against the old volume and the health check
reports `MongoConnectionPoolPausedException`.

## Add the package

In the server from quickstart 1:

```bash
dotnet add package MinApiLib.Oidc.Server.MongoDB
```

It uses the official MongoDB C# driver.

## Configure the stores

Put the connection string in the configuration, for example in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017"
  }
}
```

Then replace the calls to `AddInMemoryClients`, `AddInMemoryIdentityResources`, `AddInMemoryApiScopes` and
`AddInMemoryApiResources` with `AddMongoStores`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOidcServer(options => options.KeyManagement.Enabled = true)
    .AddMongoStores(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Mongo");
        options.DatabaseName = "oidcserver";
    });
```

`AddMongoStores` registers the client store, the resource store, the CORS policy service (which reads
`AllowedCorsOrigins` from the clients), the persisted grant store, the device flow store and, for automatic key
management (`KeyManagement.Enabled`), the signing key store. Every server instance that uses the database shares the
same keys and grants. At startup it creates the indexes, among them TTL indexes on the expiration of grants and
device codes, so MongoDB deletes expired entries without a clean-up job.

| Option | Default | |
|---|---|---|
| `ConnectionString` | | Leave it empty to use an `IMongoClient` registered by the host |
| `DatabaseName` | `oidcserver` | |
| `CollectionPrefix` | empty | For databases shared with other applications |
| `UseForConfiguration` | `true` | Clients, resources and CORS origins |
| `UseForOperationalData` | `true` | Persisted grants and device codes |
| `UseForSigningKeys` | `true` | Keys of automatic key management |
| `CreateIndexes` | `true` | Creates the indexes when the host starts |

To keep part of the data elsewhere, turn its group off. For example, `UseForConfiguration = false` together with
`AddInMemoryClients(...)` and the other in-memory calls keeps the configuration in code and only the operational
data in MongoDB.

The collections are `clients`, `identityResources`, `apiResources`, `apiScopes`, `persistedGrants`, `deviceCodes`
and `signingKeys`. The configuration documents are the models themselves (`Client`, `ApiScope`...), keyed by
`ClientId` or `Name`; the package README describes each collection.

## Seed the configuration

An empty database has no clients. `OidcServerMongoContext` exposes the collections and an `UpsertAsync` for each
configuration model, which inserts the document or replaces it. The sample server writes the clients and resources
of its `Config` class at startup with a hosted service:

```csharp
public sealed class MongoSeeder(OidcServerMongoContext context) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var client in Config.Clients)
        {
            await context.UpsertAsync(client, cancellationToken);
        }

        foreach (var resource in Config.IdentityResources)
        {
            await context.UpsertAsync(resource, cancellationToken);
        }

        foreach (var scope in Config.ApiScopes)
        {
            await context.UpsertAsync(scope, cancellationToken);
        }

        foreach (var api in Config.ApiResources)
        {
            await context.UpsertAsync(api, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

```csharp
builder.Services.AddHostedService<MongoSeeder>();
```

`OidcServerMongoContext` is in the `MinApiLib.Oidc.Server.MongoDB` namespace. The upserts are idempotent, so
restarting is safe, but they overwrite any change made in the database to the same clients and resources. That
suits a sample whose configuration lives in code. For a real deployment, seed once, or manage the configuration
with an administration tool built on the same context.

## Run the client applications

Start the server; it creates the indexes and writes the configuration. Run the client applications of the previous
quickstarts: they sign in, get tokens and call the API as before, with the configuration read from MongoDB. Refresh
tokens and consents now survive a restart of the server.

The sample server does the same, choosing the stores by the presence of the connection string:

```bash
dotnet run --project Server --launch-profile https-mongo
```

[[go back to index]](../index.md#documentation-structure)
