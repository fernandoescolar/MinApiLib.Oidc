namespace MinApiLib.Oidc.JwtBearer;

/// <summary>
/// Which access tokens a JWT bearer scheme accepts.
/// </summary>
public enum DPoPTokenMode
{
    /// <summary>
    /// Bearer tokens and DPoP-bound tokens. A DPoP-bound token still needs its proof: it is rejected as a bearer token.
    /// </summary>
    Allowed,

    /// <summary>Only DPoP-bound tokens, each with its proof.</summary>
    Required
}

/// <summary>
/// DPoP settings of a JWT bearer scheme.
/// </summary>
public sealed class DPoPJwtBearerOptions
{
    /// <summary>Which access tokens the scheme accepts. Defaults to <see cref="DPoPTokenMode.Allowed"/>.</summary>
    public DPoPTokenMode Mode { get; set; } = DPoPTokenMode.Allowed;

    /// <summary>The algorithms accepted for proofs, announced in the challenge. Defaults to ES256 and PS256.</summary>
    public ICollection<string> SupportedAlgorithms { get; set; } = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSsaPssSha256];

    /// <summary>How long after <c>iat</c> a proof is accepted. Defaults to one minute.</summary>
    public TimeSpan ProofLifetime { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The clock difference tolerated between clients and the API. Defaults to one minute.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets whether proofs must carry a nonce the API issued (RFC 9449 section 9), which limits how far ahead
    /// a stolen key can make proofs. A proof without a valid one gets <c>401</c> with <c>use_dpop_nonce</c> and a new
    /// nonce in <c>DPoP-Nonce</c>, and the client sends the request again with it. Defaults to <see langword="false"/>.
    /// </summary>
    public bool RequireNonce { get; set; }

    /// <summary>How long a nonce is accepted. Defaults to five minutes.</summary>
    public TimeSpan NonceLifetime { get; set; } = TimeSpan.FromMinutes(5);

    internal DPoPProofValidationOptions ToValidationOptions() => new()
    {
        SupportedAlgorithms = SupportedAlgorithms,
        ProofLifetime = ProofLifetime,
        ClockSkew = ClockSkew
    };
}
