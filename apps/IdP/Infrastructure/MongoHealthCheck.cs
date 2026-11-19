using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdP.Infrastructure;

/// <summary>Pings the database for <c>/health</c>.</summary>
public sealed class MongoHealthCheck(IdPMongoContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (MongoException ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is not reachable", ex);
        }
    }
}
