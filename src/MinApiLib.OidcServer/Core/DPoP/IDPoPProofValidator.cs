namespace MinApiLib.OidcServer.Core.DPoP;

/// <summary>
/// Validates DPoP proofs (RFC 9449).
/// </summary>
public interface IDPoPProofValidator
{
    /// <summary>
    /// Validates a DPoP proof.
    /// </summary>
    /// <param name="context">The proof and the request it must match.</param>
    Task<DPoPProofValidationResult> ValidateAsync(DPoPProofValidationContext context);
}

/// <summary>
/// The input of DPoP proof validation.
/// </summary>
public class DPoPProofValidationContext
{
    /// <summary>
    /// Gets or sets the proof JWT from the <c>DPoP</c> header.
    /// </summary>
    public string ProofToken { get; set; } = default!;

    /// <summary>
    /// Gets or sets the HTTP method of the request (<c>htm</c>).
    /// </summary>
    public string Method { get; set; } = default!;

    /// <summary>
    /// Gets or sets the URL of the request without query and fragment (<c>htu</c>).
    /// </summary>
    public string Url { get; set; } = default!;

    /// <summary>
    /// Gets or sets the access token sent with the request. When set, the proof must carry its hash (<c>ath</c>).
    /// </summary>
    public string? AccessToken { get; set; }
}

/// <summary>
/// The result of DPoP proof validation.
/// </summary>
public class DPoPProofValidationResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the SHA-256 JWK thumbprint (RFC 7638) of the proof key, base64url encoded.
    /// </summary>
    public string? JsonWebKeyThumbprint { get; set; }

    /// <summary>
    /// Gets the confirmation claim (<c>cnf</c>) value that binds a token to the proof key.
    /// </summary>
    public string? Confirmation => JsonWebKeyThumbprint == null
        ? null
        : JsonSerializer.Serialize(new Dictionary<string, string> { { JwtClaimTypes.ConfirmationMethods.JwkThumbprint, JsonWebKeyThumbprint } });
}
