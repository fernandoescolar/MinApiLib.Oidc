namespace Samples.WebClient.DPoP;

/// <summary>
/// Creates DPoP proofs (RFC 9449 section 4.2): a JWT signed by the session key, for one HTTP request.
/// </summary>
public static class DPoPProof
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <param name="key">The session key.</param>
    /// <param name="method">The HTTP method of the request (<c>htm</c>).</param>
    /// <param name="url">The URL of the request; the query and fragment are left out (<c>htu</c>).</param>
    /// <param name="accessToken">The access token sent with the request to a resource server, hashed into <c>ath</c>.</param>
    public static string Create(DPoPKey key, string method, Uri url, string? accessToken = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["htm"] = method,
            ["htu"] = url.GetLeftPart(UriPartial.Path),
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (accessToken != null)
        {
            claims["ath"] = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)));
        }

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            TokenType = "dpop+jwt",
            Claims = claims,
            AdditionalHeaderClaims = new Dictionary<string, object> { ["jwk"] = key.PublicJwk },
            SigningCredentials = new SigningCredentials(key.SecurityKey, SecurityAlgorithms.EcdsaSha256)
        });
    }
}
