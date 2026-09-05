namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validation result for device code validation request.
/// </summary>
public class DeviceCodeValidationContext
{
    /// <summary>
    /// Gets or sets the device code.
    /// </summary>
    /// <value>
    /// The device code.
    /// </value>
    public string DeviceCode { get; set; } = default!;

    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    /// <value>
    /// The request.
    /// </value>
    public ValidatedTokenRequest Request { get; set; } = default!;

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    /// <value>
    /// The result.
    /// </value>
    public TokenRequestValidationResult Result { get; set; } = default!;
}
