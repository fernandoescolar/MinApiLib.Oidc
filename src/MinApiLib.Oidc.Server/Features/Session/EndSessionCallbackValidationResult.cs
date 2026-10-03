namespace MinApiLib.Oidc.Server.Features.Session;

/// <summary>
/// Validation result for end session callback requests.
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Validation.ValidationResult" />
public class EndSessionCallbackValidationResult : ValidationResult
{
    /// <summary>
    /// Gets the client front-channel logout urls.
    /// </summary>
    public IEnumerable<string>? FrontChannelLogoutUrls { get; set; }
}
