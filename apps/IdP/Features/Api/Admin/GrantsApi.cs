using MinApiLib.Oidc.Server.Core.Keys;

namespace IdP.Features.Api.Admin;

// Sessions and tokens (/api/admin/grants): the persisted grants of the server (refresh tokens, reference tokens,
// authorization codes, consents, device codes) with the user and client they belong to; and the signing keys.

public sealed record GrantModel(string Key, string Type, string? SubjectId, string? SubjectName, string? SessionId, string ClientId, string? ClientName, string? Description,
    DateTime CreationTime, DateTime? Expiration, DateTime? ConsumedTime);

public sealed record ListGrants() : GetHandlerAsync<ListGrants.Request>("/api/admin/grants")
{
    public readonly record struct Request([FromQuery] string? SubjectId, [FromQuery] string? ClientId, [FromQuery] string? Type, [FromQuery] string? SessionId,
        [FromQuery] int? Page, [FromQuery] int? PageSize, [FromServices] OidcServerMongoContext Oidc, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filters = new List<FilterDefinition<PersistedGrantDocument>>();
        if (!string.IsNullOrEmpty(request.SubjectId))
        {
            filters.Add(Builders<PersistedGrantDocument>.Filter.Eq(g => g.SubjectId, request.SubjectId));
        }

        if (!string.IsNullOrEmpty(request.ClientId))
        {
            filters.Add(Builders<PersistedGrantDocument>.Filter.Eq(g => g.ClientId, request.ClientId));
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            filters.Add(Builders<PersistedGrantDocument>.Filter.Eq(g => g.Type, request.Type));
        }

        if (!string.IsNullOrEmpty(request.SessionId))
        {
            filters.Add(Builders<PersistedGrantDocument>.Filter.Eq(g => g.SessionId, request.SessionId));
        }

        var filter = filters.Count == 0 ? FilterDefinition<PersistedGrantDocument>.Empty : Builders<PersistedGrantDocument>.Filter.And(filters);
        var total = await request.Oidc.PersistedGrants.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var grants = await request.Oidc.PersistedGrants.Find(filter).SortByDescending(g => g.CreationTime).Skip(Paging.Skip(request.Page, request.PageSize)).Limit(Paging.Size(request.PageSize)).ToListAsync(cancellationToken);

        var subjectIds = grants.Select(g => g.SubjectId).Where(s => s != null).Distinct().ToList();
        var users = subjectIds.Count == 0 ? [] : await request.Db.Users.Find(u => subjectIds.Contains(u.Id)).Project(u => new { u.Id, u.UserName }).ToListAsync(cancellationToken);
        var clientIds = grants.Select(g => g.ClientId).Distinct().ToList();
        var clients = clientIds.Count == 0 ? [] : await request.Oidc.Clients.Find(c => clientIds.Contains(c.ClientId)).Project(c => new { c.ClientId, c.ClientName }).ToListAsync(cancellationToken);

        var items = grants.Select(g => new GrantModel(g.Id, g.Type, g.SubjectId, users.FirstOrDefault(u => u.Id == g.SubjectId)?.UserName, g.SessionId, g.ClientId,
            clients.FirstOrDefault(c => c.ClientId == g.ClientId)?.ClientName, g.Description, g.CreationTime, g.Expiration, g.ConsumedTime)).ToList();
        return Results.Ok(new Page<GrantModel>(items, total, Paging.Number(request.Page), Paging.Size(request.PageSize)));
    }
}

public sealed record RevokeGrant() : DeleteHandlerAsync<RevokeGrant.Request>("/api/admin/grants/{key}")
{
    public readonly record struct Request(HttpContext HttpContext, [FromRoute] string Key, [FromServices] IPersistedGrantStore Grants, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await request.Grants.RemoveAsync(request.Key);
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Grant revoked", request.Key);
        return Results.NoContent();
    }
}

/// <summary>Revokes every grant of a user, a client, or a user at a client.</summary>
public sealed record RevokeGrants() : DeleteHandlerAsync<RevokeGrants.Request>("/api/admin/grants")
{
    public readonly record struct Request(HttpContext HttpContext, [FromQuery] string? SubjectId, [FromQuery] string? ClientId, [FromQuery] string? SessionId,
        [FromServices] IPersistedGrantStore Grants, [FromServices] IEventService Events);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SubjectId) && string.IsNullOrEmpty(request.ClientId) && string.IsNullOrEmpty(request.SessionId))
        {
            return ApiErrors.Validation("Give a subjectId, a clientId or a sessionId.");
        }

        await request.Grants.RemoveAllAsync(new PersistedGrantFilter { SubjectId = request.SubjectId, ClientId = request.ClientId, SessionId = request.SessionId });
        await Audit.RaiseAsync(request.Events, request.HttpContext, "Grants revoked", $"subject={request.SubjectId} client={request.ClientId} session={request.SessionId}", request.SubjectId, clientId: request.ClientId);
        return Results.NoContent();
    }
}

/// <summary>The signing keys of automatic key management: ids, algorithms and ages. The public parts are at the JWKS endpoint.</summary>
public sealed record ListKeys() : GetHandlerAsync<ListKeys.Request>("/api/admin/keys")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] ISigningKeyStore Store, [FromServices] IIssuerNameService Issuer);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var keys = await request.Store.LoadKeysAsync();
        return Results.Ok(new
        {
            jwksUri = (await request.Issuer.GetCurrentAsync()).TrimEnd('/') + "/.well-known/openid-configuration/jwks",
            keys = keys.OrderByDescending(k => k.Created).Select(k => new { k.Id, k.Created, k.Algorithm, k.DataProtected, k.Version })
        });
    }
}
