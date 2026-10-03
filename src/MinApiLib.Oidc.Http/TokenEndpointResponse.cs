namespace MinApiLib.Oidc.Http;

/// <summary>
/// The answer of a token endpoint (RFC 6749 sections 5.1 and 5.2).
/// </summary>
/// <param name="Json">The body, to read other members.</param>
/// <param name="AccessToken">The <c>access_token</c>.</param>
/// <param name="TokenType">The <c>token_type</c>: <c>Bearer</c> or <c>DPoP</c>.</param>
/// <param name="ExpiresIn">The <c>expires_in</c>, in seconds.</param>
/// <param name="RefreshToken">The <c>refresh_token</c>.</param>
/// <param name="Error">The <c>error</c>, or <c>http_</c> and the status code when an error status has none.</param>
/// <param name="ErrorDescription">The <c>error_description</c>.</param>
public sealed record TokenEndpointResponse(
    string Json,
    string? AccessToken,
    string? TokenType,
    int? ExpiresIn,
    string? RefreshToken,
    string? Error,
    string? ErrorDescription)
{
    /// <summary>Gets whether the request failed: an error, or no access token.</summary>
    public bool IsError => Error != null || AccessToken == null;

    internal static TokenEndpointResponse Parse(HttpStatusCode status, string body)
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

    /// <summary>The error as an exception.</summary>
    public AccessTokenException ToException() => new(Error ?? "invalid_response", Error == null ? "The token endpoint answered without an access token." : ErrorDescription);

    /// <summary>When the token expires, counting <see cref="ExpiresIn"/> from <paramref name="now"/>.</summary>
    /// <param name="now">When the response arrived.</param>
    public DateTimeOffset? ExpiresAt(DateTimeOffset now) => ExpiresIn == null ? null : now.AddSeconds(ExpiresIn.Value);

    private static TokenEndpointResponse Invalid(HttpStatusCode status)
        => new(string.Empty, null, null, null, null, "invalid_response", $"The token endpoint answered {(int)status} without a JSON object.");
}
