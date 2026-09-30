namespace MinApiLib.OidcServer.Client.DPoP;

/// <summary>
/// Creates DPoP proofs (RFC 9449 section 4.2): a JWT, signed by the client's key, for one HTTP request.
/// </summary>
public static class DPoPProof
{
    /// <summary>The name of the HTTP header that carries the proof.</summary>
    public const string HeaderName = "DPoP";

    /// <summary>The name of the HTTP header with which servers send the nonce to put in the next proof.</summary>
    public const string NonceHeaderName = "DPoP-Nonce";

    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <summary>Creates a proof for one request.</summary>
    /// <param name="key">The key the proof shows possession of.</param>
    /// <param name="method">The HTTP method of the request (<c>htm</c>).</param>
    /// <param name="url">The URL of the request (<c>htu</c>). The query and the fragment are left out.</param>
    /// <param name="accessToken">
    /// The access token sent with the request, for requests to resource servers. Its hash goes in <c>ath</c>.
    /// </param>
    /// <param name="nonce">The last nonce the server sent, if it requires one (<c>nonce</c>).</param>
    /// <param name="issuedAt">The time of the proof (<c>iat</c>). Defaults to now.</param>
    public static string Create(DPoPKey key, HttpMethod method, Uri url, string? accessToken = null, string? nonce = null, DateTimeOffset? issuedAt = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);

        var claims = new Dictionary<string, object>
        {
            ["jti"] = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16)),
            ["htm"] = method.Method,
            ["htu"] = url.GetLeftPart(UriPartial.Path),
            ["iat"] = (issuedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds()
        };

        if (accessToken != null)
        {
            claims["ath"] = HashAccessToken(accessToken);
        }

        if (nonce != null)
        {
            claims["nonce"] = nonce;
        }

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = "dpop+jwt",
            Claims = claims,
            AdditionalHeaderClaims = new Dictionary<string, object> { ["jwk"] = key.PublicJwk },
            SigningCredentials = new SigningCredentials(key.SecurityKey, key.Algorithm)
        });
    }

    /// <summary>The <c>ath</c> value of an access token: the base64url SHA-256 of its ASCII bytes.</summary>
    public static string HashAccessToken(string accessToken)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)));
    }
}
