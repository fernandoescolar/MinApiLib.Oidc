namespace MinApiLib.Oidc.Server.Features.Session;

/// <summary>
/// Validation result for end session requests
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Validation.ValidationResult" />
public class EndSessionValidationResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the validated request.
    /// </summary>
    /// <value>
    /// The validated request.
    /// </value>
    public ValidatedEndSessionRequest ValidatedRequest { get; set; } = default!;
}
