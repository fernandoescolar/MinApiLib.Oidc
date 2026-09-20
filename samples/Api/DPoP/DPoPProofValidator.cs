namespace Samples.Api.DPoP;

/// <summary>
/// Validates the DPoP proof of a request to the API (RFC 9449 section 4.3 and section 7).
/// </summary>
public sealed class DPoPProofValidator(IMemoryCache replayCache, TimeProvider time)
{
    public static readonly string[] SupportedAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSsaPssSha256];

    private static readonly TimeSpan Validity = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);
    private static readonly JsonWebTokenHandler Handler = new();

    /// <summary>
    /// Validates the proof and returns the thumbprint of its key, to compare with <c>cnf.jkt</c> of the access token.
    /// </summary>
    /// <returns>The thumbprint, or the reason the proof is not valid.</returns>
    public async Task<(string? Thumbprint, string? Error)> ValidateAsync(string proof, string method, Uri requestUrl, string accessToken)
    {
        JsonWebToken jwt;
        try
        {
            jwt = Handler.ReadJsonWebToken(proof);
        }
        catch (ArgumentException)
        {
            return (null, "Malformed DPoP proof");
        }

        // 1. A proof: typ dpop+jwt, an asymmetric algorithm and the public key in the jwk header.
        if (jwt.Typ != "dpop+jwt")
        {
            return (null, "DPoP proof must have typ dpop+jwt");
        }

        if (!SupportedAlgorithms.Contains(jwt.Alg))
        {
            return (null, "DPoP proof uses an unsupported algorithm");
        }

        if (!jwt.TryGetHeaderValue<JsonElement>("jwk", out var jwkElement) || jwkElement.ValueKind != JsonValueKind.Object)
        {
            return (null, "DPoP proof has no jwk header");
        }

        var jwk = new JsonWebKey(jwkElement.GetRawText());
        if (jwk.HasPrivateKey)
        {
            return (null, "DPoP proof jwk must be a public key");
        }

        // 2. Signed by that key.
        var signature = await Handler.ValidateTokenAsync(proof, new TokenValidationParameters
        {
            IssuerSigningKey = jwk,
            ValidAlgorithms = SupportedAlgorithms,
            ValidTypes = ["dpop+jwt"],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false
        });
        if (!signature.IsValid)
        {
            return (null, "Invalid DPoP proof signature");
        }

        // 3. For this request: method and URL, without query and fragment.
        if (!jwt.TryGetPayloadValue<string>("htm", out var htm) || htm != method)
        {
            return (null, "DPoP proof htm does not match the request method");
        }

        if (!jwt.TryGetPayloadValue<string>("htu", out var htu) || !Uri.TryCreate(htu, UriKind.Absolute, out var htuUrl) ||
            Uri.Compare(htuUrl, requestUrl, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return (null, "DPoP proof htu does not match the request URL");
        }

        // 4. Recent.
        if (!jwt.TryGetPayloadValue<long>("iat", out var iat))
        {
            return (null, "DPoP proof has no iat");
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
        var now = time.GetUtcNow();
        if (issuedAt > now + ClockSkew || issuedAt + Validity + ClockSkew < now)
        {
            return (null, "DPoP proof iat is outside the accepted window");
        }

        // 5. For this access token.
        var expectedAth = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken)));
        if (!jwt.TryGetPayloadValue<string>("ath", out var ath) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(ath), Encoding.ASCII.GetBytes(expectedAth)))
        {
            return (null, "DPoP proof ath does not match the access token");
        }

        // 6. Used once: a captured proof cannot be replayed while it is still recent.
        if (!jwt.TryGetPayloadValue<string>("jti", out var jti) || string.IsNullOrEmpty(jti))
        {
            return (null, "DPoP proof has no jti");
        }

        var replayKey = "dpop:" + jti;
        if (replayCache.TryGetValue(replayKey, out _))
        {
            return (null, "DPoP proof has already been used");
        }

        replayCache.Set(replayKey, true, Validity + ClockSkew + ClockSkew);

        return (Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint()), null);
    }
}
