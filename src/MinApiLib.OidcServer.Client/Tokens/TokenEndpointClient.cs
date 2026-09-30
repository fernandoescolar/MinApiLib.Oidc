namespace MinApiLib.OidcServer.Client.Tokens;

/// <summary>
/// Posts token requests, with a DPoP proof when there is a key. When the server asks for a nonce
/// (<c>use_dpop_nonce</c>, RFC 9449 section 8), it sends the request again, once, with the nonce.
/// </summary>
internal sealed partial class TokenEndpointClient(DPoPNonceStore nonces, TimeProvider time, ILogger<TokenEndpointClient> logger)
{
    public async Task<TokenEndpointResponse> RequestAsync(
        HttpClient http,
        Uri endpoint,
        IEnumerable<KeyValuePair<string, string>> parameters,
        DPoPKey? key,
        CancellationToken cancellationToken)
    {
        var form = parameters.ToList();
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new FormUrlEncodedContent(form) };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (key != null)
            {
                request.Headers.Add(DPoPProof.HeaderName, DPoPProof.Create(key, HttpMethod.Post, endpoint, nonce: nonces.Get(endpoint), issuedAt: time.GetUtcNow()));
            }

            using var response = await http.SendAsync(request, cancellationToken);
            var hasNewNonce = key != null && nonces.Update(endpoint, response);
            var result = TokenEndpointResponse.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

            if (key != null && attempt == 0 && hasNewNonce && result.Error == "use_dpop_nonce")
            {
                LogNonceRetry(logger, endpoint);
                continue;
            }

            if (result.IsError)
            {
                LogTokenRequestFailed(logger, endpoint, result.Error, result.ErrorDescription);
            }

            return result;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The token endpoint {Endpoint} asked for a DPoP nonce: sending the request again with it")]
    private static partial void LogNonceRetry(ILogger logger, Uri endpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The token endpoint {Endpoint} answered {Error}: {ErrorDescription}")]
    private static partial void LogTokenRequestFailed(ILogger logger, Uri endpoint, string? error, string? errorDescription);
}
