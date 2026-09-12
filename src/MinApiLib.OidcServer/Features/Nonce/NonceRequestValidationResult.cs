namespace MinApiLib.OidcServer.Features.Nonce;

/// <summary>
/// Validation result for nonce requests
/// </summary>
public class NonceRequestValidationResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the token validation result.
    /// </summary>
    /// <value>
    /// The token validation result.
    /// </value>
    public TokenValidationResult TokenValidationResult { get; set; } = default!;

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    /// <value>
    /// The subject.
    /// </value>
    public ClaimsPrincipal Subject { get; set; } = default!;


    /// <summary>
    /// Gets or sets the session id.
    /// </summary>
    public string? SessionId { get; set; }

}
