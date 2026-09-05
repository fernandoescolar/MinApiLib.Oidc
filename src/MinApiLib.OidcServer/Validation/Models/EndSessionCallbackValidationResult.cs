namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validation result for end session callback requests.
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Validation.ValidationResult" />
public class EndSessionCallbackValidationResult : ValidationResult
{
    /// <summary>
    /// Gets the client front-channel logout urls.
    /// </summary>
    public IEnumerable<string>? FrontChannelLogoutUrls { get; set; }
}
