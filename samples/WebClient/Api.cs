namespace Samples.WebClient;

/// <summary>
/// Calls the orders API with the user's access token.
/// </summary>
public static class Api
{
    /// <summary>The API client with <c>AddUserAccessTokenHandler</c>: the way to call the API.</summary>
    public const string HttpClientName = "api";

    /// <summary>The API client without it, for the requests the attacker builds by hand.</summary>
    public const string RawHttpClientName = "api-raw";

    /// <param name="mode">
    /// <c>normal</c> calls the API as the client should, through the token handler; <c>bearer</c> sends a DPoP-bound
    /// token as a bearer token; <c>other-key</c> signs the proof with another key, as someone who stole the token
    /// would; <c>replay</c> sends the same proof twice.
    /// </param>
    public static async Task<IReadOnlyList<ExchangeRecorder.Exchange>> CallAsync(HttpContext http, IHttpClientFactory clients, IUserAccessTokenManager tokens, string mode, CancellationToken cancellationToken)
    {
        if (mode == "normal")
        {
            // All it takes: the handler adds the token, and for DPoP a new proof of the session key.
            using var response = await clients.CreateClient(HttpClientName).GetAsync("/orders", cancellationToken);
            return ExchangeRecorder.Recorded(http);
        }

        var token = await tokens.GetAccessTokenAsync(http, Schemes.Cookie, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Not signed in.");
        var raw = clients.CreateClient(RawHttpClientName);
        var url = new Uri(raw.BaseAddress!, "/orders");

        string? proof = null;
        if (mode == "other-key")
        {
            using var otherKey = DPoPKey.CreateECDsa();
            proof = DPoPProof.Create(otherKey, HttpMethod.Get, url, token.Value);
        }
        else if (mode == "replay" && token.IsDPoP)
        {
            proof = DPoPProof.Create(token.DPoPKey!, HttpMethod.Get, url, token.Value);
        }

        var scheme = mode == "bearer" ? AccessToken.BearerTokenType : token.Scheme;
        for (var i = 0; i < (mode == "replay" ? 2 : 1); i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Authorization", $"{scheme} {token.Value}");
            if (proof != null)
            {
                request.Headers.Add(DPoPConstants.HeaderName, proof);
            }

            using var response = await raw.SendAsync(request, cancellationToken);
        }

        return ExchangeRecorder.Recorded(http);
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
