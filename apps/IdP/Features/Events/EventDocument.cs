namespace IdP.Features.Events;

/// <summary>An audit event, as stored in MongoDB: the fields the administration console filters on, and the whole event as a document.</summary>
public sealed class EventDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string Category { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string EventType { get; set; } = default!;

    public int EventId { get; set; }

    public string? Message { get; set; }

    public DateTime TimeStamp { get; set; }

    public string? ActivityId { get; set; }

    public string? RemoteIpAddress { get; set; }

    public string? SubjectId { get; set; }

    public string? ClientId { get; set; }

    public string? Username { get; set; }

    public BsonDocument Details { get; set; } = [];
}
