namespace MinApiLib.Oidc.DPoP;

/// <summary>
/// What a <see cref="DPoPProofValidator"/> accepts.
/// </summary>
public sealed class DPoPProofValidationOptions
{
    /// <summary>The algorithms accepted for proofs. Defaults to ES256 and PS256.</summary>
    public ICollection<string> SupportedAlgorithms { get; set; } = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSsaPssSha256];

    /// <summary>How long after <c>iat</c> a proof is accepted. Defaults to one minute.</summary>
    public TimeSpan ProofLifetime { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The clock difference tolerated between the client and the server. Defaults to one minute.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// The outcome of <see cref="DPoPProofValidator.ValidateAsync"/>.
/// </summary>
public sealed record DPoPProofValidationResult
{
    /// <summary>Why the proof was rejected, or <see langword="null"/> when it is valid.</summary>
    public string? Error { get; init; }

    /// <summary>Gets whether the proof is valid.</summary>
    public bool IsValid => Error == null;

    /// <summary>The RFC 7638 thumbprint of the proof's key, to compare with <c>cnf.jkt</c> of the access token.</summary>
    public string? Thumbprint { get; init; }

    /// <summary>The <c>nonce</c> of the proof, for servers that issue nonces.</summary>
    public string? Nonce { get; init; }

    /// <summary>The exception behind <see cref="Error"/>, when reading the proof or checking its signature threw, for logging.</summary>
    public Exception? Exception { get; init; }

    internal static DPoPProofValidationResult Failed(string error, Exception? exception = null) => new() { Error = error, Exception = exception };
}

/// <summary>
/// Validates DPoP proofs (RFC 9449 section 4.3): the form of the proof, its signature by the key in its header, the
/// request it was made for, its age, the access token it goes with, and that it is used once.
/// </summary>
/// <param name="options">What the validator accepts.</param>
/// <param name="replayCache">The proofs accepted before.</param>
/// <param name="time">The clock.</param>
public sealed class DPoPProofValidator(DPoPProofValidationOptions options, IDPoPReplayCache replayCache, TimeProvider time)
{
    private const string ReplayKeyPrefix = "dpop:";

    private static readonly JsonWebTokenHandler Handler = new();

    /// <summary>Validates the proof of one request.</summary>
    /// <param name="proof">The <c>DPoP</c> header.</param>
    /// <param name="method">The HTTP method of the request.</param>
    /// <param name="url">The URL of the request. Its query and fragment are ignored.</param>
    /// <param name="accessToken">The access token sent with the request, which <c>ath</c> must match. Leave it out at a token endpoint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<DPoPProofValidationResult> ValidateAsync(string proof, string method, Uri url, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri)
        {
            throw new ArgumentException("The request URL must be absolute.", nameof(url));
        }

        JsonWebToken jwt;
        try
        {
            jwt = Handler.ReadJsonWebToken(proof);
        }
        catch (Exception ex) when (ex is ArgumentException or SecurityTokenMalformedException)
        {
            return DPoPProofValidationResult.Failed("Malformed DPoP proof", ex);
        }

        // A proof: typ dpop+jwt, an accepted asymmetric algorithm and a public key in the jwk header.
        if (jwt.Typ != DPoPConstants.ProofTokenType)
        {
            return DPoPProofValidationResult.Failed("DPoP proof must have typ dpop+jwt");
        }

        if (!options.SupportedAlgorithms.Contains(jwt.Alg))
        {
            return DPoPProofValidationResult.Failed("DPoP proof uses an unsupported algorithm");
        }

        if (!jwt.TryGetHeaderValue<JsonElement>(DPoPConstants.Claims.JsonWebKey, out var jwkElement) || jwkElement.ValueKind != JsonValueKind.Object)
        {
            return DPoPProofValidationResult.Failed("DPoP proof has no jwk header");
        }

        JsonWebKey jwk;
        try
        {
            jwk = new JsonWebKey(jwkElement.GetRawText());
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            return DPoPProofValidationResult.Failed("DPoP proof has an invalid jwk header", ex);
        }

        if (jwk.HasPrivateKey || jwkElement.TryGetProperty("d", out _) || jwk.Kty is not (JsonWebAlgorithmsKeyTypes.EllipticCurve or JsonWebAlgorithmsKeyTypes.RSA))
        {
            return DPoPProofValidationResult.Failed("DPoP proof jwk must be a public RSA or EC key");
        }

        // Signed by that key.
        var signature = await Handler.ValidateTokenAsync(proof, new TokenValidationParameters
        {
            IssuerSigningKey = jwk,
            ValidAlgorithms = options.SupportedAlgorithms,
            ValidTypes = [DPoPConstants.ProofTokenType],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false,
            RequireSignedTokens = true
        });
        if (!signature.IsValid)
        {
            return DPoPProofValidationResult.Failed("Invalid DPoP proof signature", signature.Exception);
        }

        // For this request: method and URL, without query and fragment.
        if (!jwt.TryGetPayloadValue<string>(DPoPConstants.Claims.HttpMethod, out var htm) || htm != method)
        {
            return DPoPProofValidationResult.Failed("DPoP proof htm does not match the request method");
        }

        if (!jwt.TryGetPayloadValue<string>(DPoPConstants.Claims.HttpUrl, out var htu) || !UrlsMatch(htu, url))
        {
            return DPoPProofValidationResult.Failed("DPoP proof htu does not match the request URL");
        }

        // Recent.
        if (!jwt.TryGetPayloadValue<long>(DPoPConstants.Claims.IssuedAt, out var iat))
        {
            return DPoPProofValidationResult.Failed("DPoP proof has no iat");
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
        var now = time.GetUtcNow();
        if (issuedAt > now + options.ClockSkew || issuedAt + options.ProofLifetime + options.ClockSkew < now)
        {
            return DPoPProofValidationResult.Failed("DPoP proof iat is outside the accepted window");
        }

        // For this access token.
        if (accessToken != null &&
            (!jwt.TryGetPayloadValue<string>(DPoPConstants.Claims.AccessTokenHash, out var ath) ||
             !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(ath), Encoding.ASCII.GetBytes(DPoPProof.HashAccessToken(accessToken)))))
        {
            return DPoPProofValidationResult.Failed("DPoP proof ath does not match the access token");
        }

        // Used once: a captured proof cannot be replayed while it is still recent.
        if (!jwt.TryGetPayloadValue<string>(DPoPConstants.Claims.JwtId, out var jti) || string.IsNullOrEmpty(jti))
        {
            return DPoPProofValidationResult.Failed("DPoP proof has no jti");
        }

        var thumbprint = Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
        var expiresAt = issuedAt + options.ProofLifetime + options.ClockSkew + options.ClockSkew;
        if (!await replayCache.TryAddAsync(ReplayKeyPrefix + thumbprint + "." + jti, expiresAt, cancellationToken))
        {
            return DPoPProofValidationResult.Failed("DPoP proof has already been used");
        }

        return new DPoPProofValidationResult
        {
            Thumbprint = thumbprint,
            Nonce = jwt.TryGetPayloadValue<string>(DPoPConstants.Claims.Nonce, out var nonce) ? nonce : null
        };
    }

    /// <summary>
    /// RFC 9449 section 4.3 item 9: <c>htu</c> matches the request URL without query and fragment. Scheme and host are
    /// compared case-insensitively, the path exactly.
    /// </summary>
    private static bool UrlsMatch(string htu, Uri url)
        => Uri.TryCreate(htu, UriKind.Absolute, out var proofUrl) &&
           string.Equals(proofUrl.Scheme, url.Scheme, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(proofUrl.Host, url.Host, StringComparison.OrdinalIgnoreCase) &&
           proofUrl.Port == url.Port &&
           string.Equals(proofUrl.AbsolutePath, url.AbsolutePath, StringComparison.Ordinal);
}
