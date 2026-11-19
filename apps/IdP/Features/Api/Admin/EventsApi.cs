using System.Text.Json.Nodes;
using MongoDB.Bson.IO;

namespace IdP.Features.Api.Admin;

/// <summary>The audit log (/api/admin/events), with filters and paging.</summary>
public sealed record ListEvents() : GetHandlerAsync<ListEvents.Request>("/api/admin/events")
{
    public readonly record struct Request([FromQuery] string? Category, [FromQuery] string? Type, [FromQuery] string? Name, [FromQuery] string? SubjectId, [FromQuery] string? ClientId,
        [FromQuery] string? Q, [FromQuery] DateTime? From, [FromQuery] DateTime? To, [FromQuery] int? Page, [FromQuery] int? PageSize, [FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var f = Builders<EventDocument>.Filter;
        var filters = new List<FilterDefinition<EventDocument>>();
        if (!string.IsNullOrEmpty(request.Category))
        {
            filters.Add(f.Eq(e => e.Category, request.Category));
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            filters.Add(f.Eq(e => e.EventType, request.Type));
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filters.Add(f.Eq(e => e.Name, request.Name));
        }

        if (!string.IsNullOrEmpty(request.SubjectId))
        {
            filters.Add(f.Eq(e => e.SubjectId, request.SubjectId));
        }

        if (!string.IsNullOrEmpty(request.ClientId))
        {
            filters.Add(f.Eq(e => e.ClientId, request.ClientId));
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var regex = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(request.Q.Trim()), "i");
            filters.Add(f.Or(f.Regex(e => e.Username, regex), f.Regex(e => e.Message, regex), f.Regex(e => e.Name, regex), f.Regex(e => e.RemoteIpAddress, regex)));
        }

        if (request.From is { } from)
        {
            filters.Add(f.Gte(e => e.TimeStamp, from.ToUniversalTime()));
        }

        if (request.To is { } to)
        {
            filters.Add(f.Lte(e => e.TimeStamp, to.ToUniversalTime()));
        }

        var filter = filters.Count == 0 ? FilterDefinition<EventDocument>.Empty : f.And(filters);
        var total = await request.Db.Events.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var events = await request.Db.Events.Find(filter).SortByDescending(e => e.TimeStamp).Skip(Paging.Skip(request.Page, request.PageSize)).Limit(Paging.Size(request.PageSize)).ToListAsync(cancellationToken);
        return Results.Ok(new Page<EventModel>(events.Select(EventsApi.Model).ToList(), total, Paging.Number(request.Page), Paging.Size(request.PageSize)));
    }
}

/// <summary>The distinct categories and names, for the filters.</summary>
public sealed record EventFilters() : GetHandlerAsync<EventFilters.Request>("/api/admin/events/filters")
{
    public readonly record struct Request([FromServices] IdPMongoContext Db);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder) => builder.RequireAdmin();

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var categories = await request.Db.Events.DistinctAsync(e => e.Category, FilterDefinition<EventDocument>.Empty, cancellationToken: cancellationToken);
        var names = await request.Db.Events.DistinctAsync(e => e.Name, FilterDefinition<EventDocument>.Empty, cancellationToken: cancellationToken);
        return Results.Ok(new
        {
            categories = (await categories.ToListAsync(cancellationToken)).Order(StringComparer.Ordinal),
            names = (await names.ToListAsync(cancellationToken)).Order(StringComparer.Ordinal),
            types = new[] { "Success", "Failure", "Information", "Error" }
        });
    }
}

public sealed record EventModel(string Id, string Category, string Name, string EventType, int EventId, string? Message, DateTime TimeStamp, string? ActivityId,
    string? RemoteIpAddress, string? SubjectId, string? ClientId, string? Username, JsonNode? Details);

public static class EventsApi
{
    private static readonly JsonWriterSettings Relaxed = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    public static EventModel Model(EventDocument e)
        => new(e.Id.ToString(), e.Category, e.Name, e.EventType, e.EventId, e.Message, e.TimeStamp, e.ActivityId, e.RemoteIpAddress, e.SubjectId, e.ClientId, e.Username,
            JsonNode.Parse(e.Details.ToJson(Relaxed)));
}
