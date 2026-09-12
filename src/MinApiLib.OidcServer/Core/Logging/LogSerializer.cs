namespace MinApiLib.OidcServer.Core.Logging;

/// <summary>
/// Helper to JSON serialize object data for logging.
/// </summary>
internal static class LogSerializer
{
    static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    static LogSerializer()
    {
        Options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    /// <summary>
    /// Serializes the specified object.
    /// </summary>
    /// <param name="logObject">The object.</param>
    /// <returns></returns>
    public static string Serialize(object logObject)
    {
        return System.Text.Json.JsonSerializer.Serialize(logObject, Options);
    }
}
