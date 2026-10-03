namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// A persisted grant as stored in MongoDB. The key is the document id.
/// </summary>
public sealed class PersistedGrantDocument
{
    /// <summary>The grant key.</summary>
    public string Id { get; set; } = default!;

    /// <summary>The grant type.</summary>
    public string Type { get; set; } = default!;

    /// <summary>The subject.</summary>
    public string? SubjectId { get; set; }

    /// <summary>The session.</summary>
    public string? SessionId { get; set; }

    /// <summary>The client.</summary>
    public string ClientId { get; set; } = default!;

    /// <summary>The description given by the user.</summary>
    public string? Description { get; set; }

    /// <summary>When the grant was created (UTC).</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>When the grant expires (UTC); a TTL index deletes it afterwards.</summary>
    public DateTime? Expiration { get; set; }

    /// <summary>When the grant was consumed (UTC).</summary>
    public DateTime? ConsumedTime { get; set; }

    /// <summary>The serialized grant.</summary>
    public string Data { get; set; } = default!;

    internal static PersistedGrantDocument From(PersistedGrant grant) => new()
    {
        Id = grant.Key,
        Type = grant.Type,
        SubjectId = grant.SubjectId,
        SessionId = grant.SessionId,
        ClientId = grant.ClientId,
        Description = grant.Description,
        CreationTime = grant.CreationTime,
        Expiration = grant.Expiration,
        ConsumedTime = grant.ConsumedTime,
        Data = grant.Data
    };

    internal PersistedGrant ToModel() => new()
    {
        Key = Id,
        Type = Type,
        SubjectId = SubjectId,
        SessionId = SessionId,
        ClientId = ClientId,
        Description = Description,
        CreationTime = CreationTime,
        Expiration = Expiration,
        ConsumedTime = ConsumedTime,
        Data = Data
    };
}

/// <summary>
/// A device authorization as stored in MongoDB. The device code is the document id.
/// </summary>
public sealed class DeviceFlowDocument
{
    /// <summary>The device code.</summary>
    public string Id { get; set; } = default!;

    /// <summary>The user code, unique.</summary>
    public string UserCode { get; set; } = default!;

    /// <summary>The subject, once the user authorized the device.</summary>
    public string? SubjectId { get; set; }

    /// <summary>The session, once the user authorized the device.</summary>
    public string? SessionId { get; set; }

    /// <summary>The client.</summary>
    public string ClientId { get; set; } = default!;

    /// <summary>The description given by the user.</summary>
    public string? Description { get; set; }

    /// <summary>When the device code was created (UTC).</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>When the device code expires (UTC); a TTL index deletes it afterwards.</summary>
    public DateTime Expiration { get; set; }

    /// <summary>The serialized <see cref="DeviceCode"/>.</summary>
    public string Data { get; set; } = default!;
}

/// <summary>
/// A signing key of automatic key management as stored in MongoDB. The key id is the document id.
/// </summary>
public sealed class SigningKeyDocument
{
    /// <summary>The key id.</summary>
    public string Id { get; set; } = default!;

    /// <summary>The serialization version.</summary>
    public int Version { get; set; }

    /// <summary>When the key was created (UTC).</summary>
    public DateTime Created { get; set; }

    /// <summary>The signing algorithm.</summary>
    public string Algorithm { get; set; } = default!;

    /// <summary>The serialized key.</summary>
    public string Data { get; set; } = default!;

    /// <summary>Whether <see cref="Data"/> is protected with ASP.NET Core Data Protection.</summary>
    public bool DataProtected { get; set; }

    internal static SigningKeyDocument From(SerializedKey key) => new()
    {
        Id = key.Id,
        Version = key.Version,
        Created = key.Created,
        Algorithm = key.Algorithm,
        Data = key.Data,
        DataProtected = key.DataProtected
    };

    internal SerializedKey ToModel() => new()
    {
        Id = Id,
        Version = Version,
        Created = Created,
        Algorithm = Algorithm,
        Data = Data,
        DataProtected = DataProtected
    };
}
