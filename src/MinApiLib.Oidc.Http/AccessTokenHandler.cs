namespace MinApiLib.Oidc.Http;

/// <summary>
/// Adds an access token to the requests: <c>Authorization: Bearer</c>, or <c>Authorization: DPoP</c> with a new proof
/// for each request. On a <c>401</c>, it sends the request once more: with the nonce the server asked for
/// (<c>use_dpop_nonce</c>), or with a new token.
/// </summary>
/// <remarks>
/// <para>
/// Derive from it to send tokens from another source. Register the dependencies with
/// <c>services.AddTokenEndpointClient()</c>.
/// </para>
/// <para>
/// A request can only be sent again when its content can be read twice, as with <see cref="StringContent"/>,
/// <see cref="ByteArrayContent"/> or <c>JsonContent</c>, but not <see cref="StreamContent"/>.
/// </para>
/// </remarks>
/// <param name="nonces">The nonces servers sent, shared by every handler.</param>
/// <param name="time">The clock for the proofs.</param>
/// <param name="logger">The logger.</param>
public abstract partial class AccessTokenHandler(DPoPNonceStore nonces, TimeProvider time, ILogger logger) : DelegatingHandler
{
    /// <summary>Gets the token to send.</summary>
    /// <param name="forceRenewal">The server rejected the current token: get a new one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected abstract Task<AccessToken> GetAccessTokenAsync(bool forceRenewal, CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri ?? throw new InvalidOperationException("The request has no URL.");
        var token = await GetAccessTokenAsync(forceRenewal: false, cancellationToken);
        var (response, newNonce) = await SendWithTokenAsync(request, url, token, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && token.IsDPoP && newNonce && IsNonceChallenge(response))
        {
            LogNonceRetry(logger, url);
            response.Dispose();
            (response, _) = await SendWithTokenAsync(request, url, token, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized && !IsNonceChallenge(response))
        {
            var renewed = await GetAccessTokenAsync(forceRenewal: true, cancellationToken);
            if (renewed.Value != token.Value)
            {
                LogRenewedAfterUnauthorized(logger, url);
                response.Dispose();
                (response, _) = await SendWithTokenAsync(request, url, renewed, cancellationToken);
            }
        }

        return response;
    }

    private async Task<(HttpResponseMessage Response, bool NewNonce)> SendWithTokenAsync(HttpRequestMessage request, Uri url, AccessToken token, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.Value);
        request.Headers.Remove(DPoPConstants.HeaderName);
        if (token.IsDPoP)
        {
            request.Headers.Add(DPoPConstants.HeaderName, DPoPProof.Create(token.DPoPKey!, request.Method, url, token.Value, nonces.Get(url), time.GetUtcNow()));
        }

        var response = await base.SendAsync(request, cancellationToken);
        var newNonce = token.IsDPoP && nonces.Update(url, response);
        return (response, newNonce);
    }

    /// <summary>RFC 9449 section 9: <c>WWW-Authenticate: DPoP error=DPoPConstants.Errors.UseNonce</c>.</summary>
    private static bool IsNonceChallenge(HttpResponseMessage response)
        => response.Headers.WwwAuthenticate.Any(challenge =>
            string.Equals(challenge.Scheme, DPoPConstants.TokenType, StringComparison.OrdinalIgnoreCase) &&
            challenge.Parameter?.Contains(DPoPConstants.Errors.UseNonce, StringComparison.Ordinal) == true);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Url} asked for a DPoP nonce: sending the request again with it")]
    private static partial void LogNonceRetry(ILogger logger, Uri url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Url} rejected the access token: sending the request again with a new one")]
    private static partial void LogRenewedAfterUnauthorized(ILogger logger, Uri url);
}
