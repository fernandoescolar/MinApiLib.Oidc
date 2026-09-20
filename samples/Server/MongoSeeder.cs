namespace Samples.Server;

/// <summary>
/// Writes the sample configuration to MongoDB at startup. Upserts are idempotent, so restarting is safe.
/// </summary>
public sealed class MongoSeeder(OidcServerMongoContext context, string webClientUrl) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var client in Config.Clients(webClientUrl))
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
