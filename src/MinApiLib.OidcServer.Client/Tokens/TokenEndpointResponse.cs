namespace MinApiLib.OidcServer.Client.Tokens;

/// <summary>
/// The answer of a token endpoint (RFC 6749 sections 5.1 and 5.2).
/// </summary>
internal sealed record TokenEndpointResponse(
    string Json,
    string? AccessToken,
    string? TokenType,
    int? ExpiresIn,
    string? RefreshToken,
    string? Error,
    string? ErrorDescription)
{
    public bool IsError => Error != null || AccessToken == null;

    public static TokenEndpointResponse Parse(HttpStatusCode status, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Invalid(status);
            }

            string? String(string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            int? expiresIn = root.TryGetProperty("expires_in", out var expires) && expires.ValueKind == JsonValueKind.Number && expires.TryGetInt32(out var seconds)
                ? seconds
                : int.TryParse(String("expires_in"), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) ? seconds : null;

            var error = String("error");
            if (error == null && (int)status >= 400)
            {
                error = "http_" + ((int)status).ToString(CultureInfo.InvariantCulture);
            }

            return new TokenEndpointResponse(body, String("access_token"), String("token_type"), expiresIn, String("refresh_token"), error, String("error_description"));
        }
        catch (JsonException)
        {
            return Invalid(status);
        }
    }

    public AccessTokenException ToException() => new(Error ?? "invalid_response", Error == null ? "The token endpoint answered without an access token." : ErrorDescription);

    public DateTimeOffset? ExpiresAt(DateTimeOffset now) => ExpiresIn == null ? null : now.AddSeconds(ExpiresIn.Value);

    private static TokenEndpointResponse Invalid(HttpStatusCode status)
        => new(string.Empty, null, null, null, null, "invalid_response", $"The token endpoint answered {(int)status} without a JSON object.");
}
