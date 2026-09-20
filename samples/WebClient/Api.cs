namespace Samples.WebClient;

/// <summary>
/// Calls the orders API with the user's access token, as a bearer token or with a DPoP proof, and records what was
/// sent and received so the page can show it.
/// </summary>
public static class Api
{
    public const string HttpClientName = "api";

    public sealed record Exchange(string Request, string? Proof, int Status, string? Challenge, string Body);

    /// <param name="mode">
    /// <c>normal</c> sends the token as the client should; <c>bearer</c> sends a DPoP-bound token as a bearer token;
    /// <c>other-key</c> signs the proof with another key, as someone who stole the token would; <c>replay</c> sends
    /// the same proof twice.
    /// </param>
    public static async Task<IReadOnlyList<Exchange>> CallAsync(HttpClient http, string accessToken, string tokenType, DPoPKey? key, string mode, CancellationToken cancellationToken)
    {
        var url = new Uri(http.BaseAddress!, "/orders");
        var useDPoP = tokenType.Equals("DPoP", StringComparison.OrdinalIgnoreCase) && key != null && mode != "bearer";

        string? proof = null;
        if (useDPoP)
        {
            if (mode == "other-key")
            {
                using var otherKey = DPoPKey.Create();
                proof = DPoPProof.Create(otherKey, "GET", url, accessToken);
            }
            else
            {
                proof = DPoPProof.Create(key!, "GET", url, accessToken);
            }
        }

        var exchanges = new List<Exchange>();
        var times = mode == "replay" ? 2 : 1;
        for (var i = 0; i < times; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Authorization", $"{(useDPoP ? "DPoP" : "Bearer")} {accessToken}");
            if (proof != null)
            {
                request.Headers.Add("DPoP", proof);
            }

            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var challenge = response.Headers.WwwAuthenticate.Count == 0 ? null : string.Join(", ", response.Headers.WwwAuthenticate);

            exchanges.Add(new Exchange(Describe(request, url), proof, (int)response.StatusCode, challenge, Pretty(body)));
        }

        return exchanges;
    }

    private static string Describe(HttpRequestMessage request, Uri url)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"GET {url.PathAndQuery} HTTP/1.1\nHost: {url.Authority}\n");
        foreach (var header in request.Headers.Where(h => h.Key != "traceparent"))
        {
            var value = string.Join(", ", header.Value);
            text.Append(CultureInfo.InvariantCulture, $"{header.Key}: {Shorten(value)}\n");
        }

        return text.ToString();
    }

    private static string Shorten(string value)
    {
        var space = value.IndexOf(' ', StringComparison.Ordinal);
        var token = space < 0 ? value : value[(space + 1)..];
        var shortened = token.Length > 48 ? token[..48] + "…" : token;
        return space < 0 ? shortened : value[..space] + " " + shortened;
    }

    public static string Pretty(string json)
    {
        try
        {
            return JsonSerializer.Serialize(JsonElement.Parse(json), Jwt.Indented);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
