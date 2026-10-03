namespace MinApiLib.Oidc.Server.Core.DPoP;

/// <summary>
/// Options for DPoP, demonstrating proof of possession (RFC 9449).
/// </summary>
public class DPoPOptions
{
    /// <summary>
    /// Gets or sets how long after its <c>iat</c> a proof is accepted. Defaults to 1 minute.
    /// </summary>
    public TimeSpan ProofTokenValidityDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the tolerated difference between the client and server clocks. Defaults to 1 minute.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the signing algorithms accepted for proofs. Only asymmetric algorithms are allowed.
    /// </summary>
    public ICollection<string> SupportedSigningAlgorithms { get; set; } = new HashSet<string>(OidcServerConstants.SupportedSigningAlgorithms);
}
