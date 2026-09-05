using Microsoft.IdentityModel.JsonWebTokens;

namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validates DPoP proofs as described in RFC 9449 section 4.3.
/// </summary>
[RegisterInIServiceCollection(typeof(IDPoPProofValidator))]
public class DefaultDPoPProofValidator : IDPoPProofValidator
{
    private const string ReplayCachePurpose = "DPoPProofReplay-";
    private const string JwkHeader = "jwk";

    private readonly OidcServerOptions _options;
    private readonly IReplayCache _replayCache;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDPoPProofValidator"/> class.
    /// </summary>
    public DefaultDPoPProofValidator(OidcServerOptions options, IReplayCache replayCache, TimeProvider clock, ILogger<DefaultDPoPProofValidator> logger)
    {
        _options = options;
        _replayCache = replayCache;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task<DPoPProofValidationResult> ValidateAsync(DPoPProofValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ProofToken.IsMissing() || context.ProofToken.Length > _options.InputLengthRestrictions.Jwt)
        {
            return Invalid("Missing or too long DPoP proof");
        }

        JsonWebToken proof;
        try
        {
            proof = new JsonWebToken(context.ProofToken);
        }
        catch (Exception ex) when (ex is ArgumentException or SecurityTokenMalformedException)
        {
            RequestValidationLog.MalformedDPoPProof(_logger, ex);
            return Invalid("Malformed DPoP proof");
        }

        if (!string.Equals(proof.Typ, JwtClaimTypes.JwtTypes.DPoPProofToken, StringComparison.Ordinal))
        {
            return Invalid("DPoP proof must have typ dpop+jwt");
        }

        if (!_options.DPoP.SupportedSigningAlgorithms.Contains(proof.Alg))
        {
            return Invalid("DPoP proof uses an unsupported algorithm");
        }

        if (!proof.TryGetHeaderValue<JsonElement>(JwkHeader, out var jwkElement) || jwkElement.ValueKind != JsonValueKind.Object)
        {
            return Invalid("DPoP proof has no jwk header");
        }

        Microsoft.IdentityModel.Tokens.JsonWebKey jwk;
        try
        {
            jwk = new Microsoft.IdentityModel.Tokens.JsonWebKey(jwkElement.GetRawText());
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            RequestValidationLog.InvalidDPoPJwk(_logger, ex);
            return Invalid("DPoP proof has an invalid jwk header");
        }

        // RFC 9449 section 4.3 item 7: the key must be public and asymmetric.
        if (jwk.HasPrivateKey || jwkElement.TryGetProperty("d", out _) ||
            !(jwk.Kty == JsonWebAlgorithmsKeyTypes.RSA || jwk.Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve))
        {
            return Invalid("DPoP proof jwk must be a public RSA or EC key");
        }

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(context.ProofToken, new TokenValidationParameters
        {
            IssuerSigningKey = jwk,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false,
            RequireSignedTokens = true,
            ValidTypes = [JwtClaimTypes.JwtTypes.DPoPProofToken],
            ValidAlgorithms = _options.DPoP.SupportedSigningAlgorithms
        });

        if (!validation.IsValid)
        {
            RequestValidationLog.DPoPSignatureValidationFailed(_logger, validation.Exception);
            return Invalid("Invalid DPoP proof signature");
        }

        if (!proof.TryGetPayloadValue<string>(JwtClaimTypes.JwtId, out var jti) || jti.IsMissing())
        {
            return Invalid("DPoP proof has no jti");
        }

        if (!proof.TryGetPayloadValue<string>(JwtClaimTypes.DPoPHttpMethod, out var htm) || !string.Equals(htm, context.Method, StringComparison.Ordinal))
        {
            return Invalid("DPoP proof htm does not match the request method");
        }

        if (!proof.TryGetPayloadValue<string>(JwtClaimTypes.DPoPHttpUrl, out var htu) || !UrlsMatch(htu, context.Url))
        {
            return Invalid("DPoP proof htu does not match the request URL");
        }

        if (!proof.TryGetPayloadValue<long>(JwtClaimTypes.IssuedAt, out var iat))
        {
            return Invalid("DPoP proof has no iat");
        }

        var now = _clock.GetUtcNow();
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
        if (issuedAt > now + _options.DPoP.ClockSkew ||
            issuedAt + _options.DPoP.ProofTokenValidityDuration + _options.DPoP.ClockSkew < now)
        {
            return Invalid("DPoP proof iat is outside the accepted window");
        }

        if (context.AccessToken != null)
        {
            var expectedAth = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(context.AccessToken)));
            if (!proof.TryGetPayloadValue<string>(JwtClaimTypes.DPoPAccessTokenHash, out var ath) || !FixedTimeComparer.IsEqual(ath ?? string.Empty, expectedAth))
            {
                return Invalid("DPoP proof ath does not match the access token");
            }
        }

        var thumbprint = Base64Url.EncodeToString(jwk.ComputeJwkThumbprint());

        // RFC 9449 section 11.1: each proof is accepted once. The key is scoped to the proof key.
        var replayKey = thumbprint + "." + jti;
        if (await _replayCache.ExistsAsync(ReplayCachePurpose, replayKey))
        {
            return Invalid("DPoP proof has already been used");
        }

        await _replayCache.AddAsync(ReplayCachePurpose, replayKey, issuedAt + _options.DPoP.ProofTokenValidityDuration + _options.DPoP.ClockSkew + _options.DPoP.ClockSkew);

        return new DPoPProofValidationResult
        {
            IsError = false,
            JsonWebKeyThumbprint = thumbprint
        };
    }

    /// <summary>
    /// RFC 9449 section 4.3 item 9: htu matches the request URL without query and fragment, with scheme and host
    /// compared case-insensitively.
    /// </summary>
    private static bool UrlsMatch(string htu, string expected)
    {
        if (!Uri.TryCreate(htu, UriKind.Absolute, out var actual) || !Uri.TryCreate(expected, UriKind.Absolute, out var target))
        {
            return false;
        }

        return string.Equals(actual.Scheme, target.Scheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(actual.Host, target.Host, StringComparison.OrdinalIgnoreCase) &&
               actual.Port == target.Port &&
               string.Equals(actual.AbsolutePath, target.AbsolutePath, StringComparison.Ordinal);
    }

    private DPoPProofValidationResult Invalid(string description)
    {
        RequestValidationLog.DPoPValidationFailed(_logger, description);
        return new DPoPProofValidationResult
        {
            IsError = true,
            Error = OidcConstants.TokenErrors.InvalidDPoPProof,
            ErrorDescription = description
        };
    }
}
