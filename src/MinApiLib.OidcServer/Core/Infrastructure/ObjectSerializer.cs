namespace MinApiLib.OidcServer.Core.Infrastructure;

internal static class ObjectSerializer
{
    private static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToString(object o)
    {
        return System.Text.Json.JsonSerializer.Serialize(o, Options);
    }

    public static T? FromString<T>(string value)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(value, Options);
    }
}
