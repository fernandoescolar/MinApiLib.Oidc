namespace MinApiLib.Oidc.Http;

/// <summary>
/// Posts token requests, with a DPoP proof when there is a key. When the server asks for a nonce
/// (<c>use_dpop_nonce</c>, RFC 9449 section 8), it sends the request again, once, with the nonce.
/// </summary>
/// <param name="nonces">The nonces servers sent, shared by every handler.</param>
/// <param name="time">The clock for the proofs.</param>
/// <param name="logger">The logger.</param>
public sealed partial class TokenEndpointClient(DPoPNonceStore nonces, TimeProvider time, ILogger<TokenEndpointClient> logger)
{
    /// <summary>Posts a token request.</summary>
    /// <param name="http">The HTTP client to send it with.</param>
    /// <param name="endpoint">The token endpoint.</param>
    /// <param name="parameters">The form parameters, client authentication included.</param>
    /// <param name="key">The key to add a DPoP proof of, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answer, successful or not. HTTP and protocol errors do not throw.</returns>
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
                request.Headers.Add(DPoPConstants.HeaderName, DPoPProof.Create(key, HttpMethod.Post, endpoint, nonce: nonces.Get(endpoint), issuedAt: time.GetUtcNow()));
            }

            using var response = await http.SendAsync(request, cancellationToken);
            var hasNewNonce = key != null && nonces.Update(endpoint, response);
            var result = TokenEndpointResponse.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

            if (key != null && attempt == 0 && hasNewNonce && result.Error == DPoPConstants.Errors.UseNonce)
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
