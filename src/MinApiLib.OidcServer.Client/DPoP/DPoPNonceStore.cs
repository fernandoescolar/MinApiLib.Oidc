namespace MinApiLib.OidcServer.Client.DPoP;

/// <summary>
/// The last <c>DPoP-Nonce</c> each server sent (RFC 9449 section 8 and 9), by origin, to put in the next proofs.
/// </summary>
internal sealed class DPoPNonceStore
{
    private readonly ConcurrentDictionary<string, string> _nonces = new(StringComparer.OrdinalIgnoreCase);

    public string? Get(Uri url) => _nonces.TryGetValue(Origin(url), out var nonce) ? nonce : null;

    /// <summary>Keeps the nonce of the response, if it has one.</summary>
    /// <returns><see langword="true"/> when the response carried a nonce.</returns>
    public bool Update(Uri url, HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(DPoPProof.NonceHeaderName, out var values) &&
            values.FirstOrDefault() is { Length: > 0 } nonce)
        {
            _nonces[Origin(url)] = nonce;
            return true;
        }

        return false;
    }

    private static string Origin(Uri url) => url.GetLeftPart(UriPartial.Authority);
}
