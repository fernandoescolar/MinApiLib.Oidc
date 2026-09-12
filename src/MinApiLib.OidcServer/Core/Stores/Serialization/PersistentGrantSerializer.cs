using System.Text.Json.Serialization.Metadata;

namespace MinApiLib.OidcServer.Core.Stores.Serialization;

/// <summary>
/// JSON-based persisted grant serializer
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Core.Stores.Serialization.IPersistentGrantSerializer" />
[RegisterInIServiceCollection(typeof(IPersistentGrantSerializer))]
public class PersistentGrantSerializer : IPersistentGrantSerializer
{
    private static readonly JsonSerializerOptions _settings;

    static PersistentGrantSerializer()
    {
        _settings = new JsonSerializerOptions()
        {
            IgnoreReadOnlyProperties = true,
            // Source-generated metadata for the built-in grants; reflection for custom DefaultGrantStore<T> types.
            TypeInfoResolver = JsonTypeInfoResolver.Combine(PersistentGrantSerializerContext.Default, new DefaultJsonTypeInfoResolver())
        };

        _settings.Converters.Add(new ClaimConverter());
        _settings.Converters.Add(new ClaimsPrincipalConverter());
    }

    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _settings);
    }

    /// <summary>
    /// Deserializes the specified string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json">The json.</param>
    /// <returns></returns>
    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _settings);
    }
}
