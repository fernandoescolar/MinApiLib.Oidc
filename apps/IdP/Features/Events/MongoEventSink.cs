namespace IdP.Features.Events;

/// <summary>
/// Writes the server's events (sign-ins, tokens, consents, errors) to the <c>events</c> collection, where the
/// administration console reads them. They are also logged, as the default sink does.
/// </summary>
public sealed class MongoEventSink(IdPMongoContext context, ILogger<MongoEventSink> logger) : IEventSink
{
    private static readonly string[] SubjectProperties = ["SubjectId"];
    private static readonly string[] ClientProperties = ["ClientId"];
    private static readonly string[] UsernameProperties = ["Username", "ClientName", "DisplayName"];

    public async Task PersistAsync(Event evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        logger.LogInformation("{@Event}", evt);

        BsonDocument details;
        try
        {
            details = BsonDocument.Parse(evt.ToString());
        }
        catch (FormatException)
        {
            details = new BsonDocument("message", evt.Message ?? string.Empty);
        }

        var document = new EventDocument
        {
            Category = evt.Category,
            Name = evt.Name,
            EventType = evt.EventType.ToString(),
            EventId = evt.Id,
            Message = evt.Message,
            TimeStamp = evt.TimeStamp == default ? DateTime.UtcNow : evt.TimeStamp,
            ActivityId = evt.ActivityId,
            RemoteIpAddress = evt.RemoteIpAddress,
            SubjectId = First(details, SubjectProperties),
            ClientId = First(details, ClientProperties),
            Username = First(details, UsernameProperties),
            Details = details
        };

        try
        {
            await context.Events.InsertOneAsync(document);
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, "The event {Name} could not be stored", evt.Name);
        }
    }

    private static string? First(BsonDocument details, string[] names)
    {
        foreach (var name in names)
        {
            if (details.TryGetValue(name, out var value) && value.IsString && !string.IsNullOrEmpty(value.AsString))
            {
                return value.AsString;
            }
        }

        return null;
    }
}

/// <summary>An event of the identity provider itself: account and administration changes.</summary>
public sealed class AuditEvent : Event
{
    public const string CategoryName = "Administration";

    public AuditEvent(string name, string? subjectId, string? username, string? actor, string message, bool success = true, string? clientId = null)
        : base(CategoryName, name, success ? EventTypes.Success : EventTypes.Failure, 10000, message)
    {
        SubjectId = subjectId;
        Username = username;
        Actor = actor;
        ClientId = clientId;
    }

    /// <summary>The user the event is about.</summary>
    public string? SubjectId { get; }

    public string? Username { get; }

    /// <summary>Who did it: the user, or an administrator.</summary>
    public string? Actor { get; }

    public string? ClientId { get; }
}
