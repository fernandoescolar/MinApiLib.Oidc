namespace IdP.Infrastructure;

/// <summary>The JSON conventions of the page models and the APIs: camelCase, enums as strings, no nulls skipped.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
