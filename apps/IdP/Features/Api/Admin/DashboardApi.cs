namespace IdP.Features.Api.Admin;

/// <summary>The numbers of the administration home page, and what the server is.</summary>
public sealed record Dashboard() : GetHandlerAsync<Dashboard.Request>("/api/admin/dashboard")
{
    public readonly record struct Request([FromServices] IdPMongoContext Db, [FromServices] OidcServerMongoContext Oidc, [FromServices] IIssuerNameService Issuer, [FromServices] IOptions<IdPOptions> Options);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dayAgo = now.AddDays(-1);
        var weekAgo = now.Date.AddDays(-6);

        var users = await request.Db.Users.CountDocumentsAsync(FilterDefinition<IdPUser>.Empty, cancellationToken: cancellationToken);
        var disabledUsers = await request.Db.Users.CountDocumentsAsync(u => !u.Enabled, cancellationToken: cancellationToken);
        var newUsers = await request.Db.Users.CountDocumentsAsync(u => u.CreatedAt >= weekAgo, cancellationToken: cancellationToken);
        var clients = await request.Oidc.Clients.CountDocumentsAsync(FilterDefinition<Client>.Empty, cancellationToken: cancellationToken);
        var disabledClients = await request.Oidc.Clients.CountDocumentsAsync(c => !c.Enabled, cancellationToken: cancellationToken);
        var providers = await request.Db.IdentityProviders.CountDocumentsAsync(p => p.Enabled, cancellationToken: cancellationToken);
        var grants = await request.Oidc.PersistedGrants.CountDocumentsAsync(g => g.Expiration == null || g.Expiration > now, cancellationToken: cancellationToken);
        var refreshTokens = await request.Oidc.PersistedGrants.CountDocumentsAsync(g => g.Type == "refresh_token" && (g.Expiration == null || g.Expiration > now), cancellationToken: cancellationToken);
        var successes = await request.Db.Events.CountDocumentsAsync(e => e.TimeStamp >= dayAgo && e.EventType == "Success", cancellationToken: cancellationToken);
        var failures = await request.Db.Events.CountDocumentsAsync(e => e.TimeStamp >= dayAgo && (e.EventType == "Failure" || e.EventType == "Error"), cancellationToken: cancellationToken);

        // Sign-ins per day, for the last week.
        var signIns = await request.Db.Events.Aggregate()
            .Match(e => e.TimeStamp >= weekAgo && e.Name == "User Login Success")
            .Group(new BsonDocument { ["_id"] = new BsonDocument("$dateToString", new BsonDocument { ["format"] = "%Y-%m-%d", ["date"] = "$TimeStamp" }), ["count"] = new BsonDocument("$sum", 1) })
            .ToListAsync(cancellationToken);
        var failedSignIns = await request.Db.Events.Aggregate()
            .Match(e => e.TimeStamp >= weekAgo && e.Name == "User Login Failure")
            .Group(new BsonDocument { ["_id"] = new BsonDocument("$dateToString", new BsonDocument { ["format"] = "%Y-%m-%d", ["date"] = "$TimeStamp" }), ["count"] = new BsonDocument("$sum", 1) })
            .ToListAsync(cancellationToken);
        var days = Enumerable.Range(0, 7).Select(i => weekAgo.AddDays(i)).Select(d =>
        {
            var key = d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            return new
            {
                date = key,
                signIns = signIns.FirstOrDefault(s => s["_id"].AsString == key)?["count"].ToInt32() ?? 0,
                failures = failedSignIns.FirstOrDefault(s => s["_id"].AsString == key)?["count"].ToInt32() ?? 0
            };
        });

        var recent = await request.Db.Events.Find(FilterDefinition<EventDocument>.Empty).SortByDescending(e => e.TimeStamp).Limit(10).ToListAsync(cancellationToken);
        var issuer = await request.Issuer.GetCurrentAsync();

        return Results.Ok(new
        {
            server = new
            {
                name = request.Options.Value.Name,
                version = FlowPages.Version,
                issuer,
                discovery = issuer.TrimEnd('/') + "/.well-known/openid-configuration",
                registrationEnabled = request.Options.Value.Registration.Enabled,
                requireEmailVerification = request.Options.Value.Registration.RequireEmailVerification,
                emailConfigured = !string.IsNullOrWhiteSpace(request.Options.Value.Email.Smtp.Host),
                eventsRetentionDays = request.Options.Value.Events.RetentionDays
            },
            counts = new { users, disabledUsers, newUsersThisWeek = newUsers, clients, disabledClients, identityProviders = providers, activeGrants = grants, refreshTokens, eventsLast24h = new { successes, failures } },
            signInsByDay = days,
            recentEvents = recent.Select(EventsApi.Model)
        });
    }
}
