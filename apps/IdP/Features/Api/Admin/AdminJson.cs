using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace IdP.Features.Api.Admin;

/// <summary>
/// The configuration models (clients, resources) travel through the administration API as they are, serialized by
/// System.Text.Json, except their secrets: those are never read back, and only their fingerprints are shown.
/// </summary>
public static class AdminJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { HideSecrets } }
    };

    private static void HideSecrets(JsonTypeInfo info)
    {
        if (info.Type != typeof(Client) && info.Type != typeof(ApiResource))
        {
            return;
        }

        foreach (var property in info.Properties.Where(p => p.PropertyType == typeof(ICollection<Secret>)))
        {
            property.ShouldSerialize = (_, _) => false;
            property.Set = null;
        }
    }

    /// <summary>The model as a JSON object, with its secrets listed by fingerprint.</summary>
    public static JsonObject With(object model, ICollection<Secret> secrets, string property = "secrets")
    {
        var node = JsonSerializer.SerializeToNode(model, Options)!.AsObject();
        node[property] = new JsonArray(secrets.Select(s => (JsonNode)JsonSerializer.SerializeToNode(SecretModel.From(s), Options)!).ToArray());
        return node;
    }

    public static T? Read<T>(JsonElement body)
    {
        try
        {
            return body.Deserialize<T>(Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}

public sealed record SecretModel(string Id, string? Description, DateTime? Expiration, string Type)
{
    public static SecretModel From(Secret secret) => new(Fingerprint(secret), secret.Description, secret.Expiration, secret.Type);

    /// <summary>Identifies a secret without revealing its (hashed) value.</summary>
    public static string Fingerprint(Secret secret) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret.Value)))[..16];

    /// <summary>A new random secret, in clear for the caller and hashed for the store.</summary>
    public static (string Clear, Secret Stored) Generate(string? description, DateTime? expiration)
    {
        var clear = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (clear, new Secret(clear.Sha256(), description ?? $"Created {DateTime.UtcNow:yyyy-MM-dd}", expiration));
    }
}

public sealed record NewSecretBody(string? Description, DateTime? Expiration, string? Value);

/// <summary>What an administrator did, for the audit log.</summary>
public static class Audit
{
    public static Task RaiseAsync(IEventService events, HttpContext http, string name, string message, string? subjectId = null, string? username = null, string? clientId = null)
        => events.RaiseAsync(new AuditEvent(name, subjectId, username, http.User.GetSubjectId(), message, clientId: clientId));
}
