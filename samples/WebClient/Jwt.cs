using System.Text.Encodings.Web;

namespace Samples.WebClient;

/// <summary>
/// Decodes a JWT for display. It does not validate it: the API and the OpenID Connect handler do.
/// </summary>
public static class Jwt
{
    // Relaxed escaping keeps "dpop+jwt" readable; the pages HTML-encode everything they show.
    public static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static (string Header, string Payload) Decode(string jwt)
    {
        var parts = jwt.Split('.');
        return (Part(parts[0]), parts.Length > 1 ? Part(parts[1]) : string.Empty);
    }

    private static string Part(string part)
        => JsonSerializer.Serialize(JsonElement.Parse(Base64UrlEncoder.Decode(part)), Indented);
}
