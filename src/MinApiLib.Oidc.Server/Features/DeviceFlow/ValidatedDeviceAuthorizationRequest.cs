namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// Models a validated request to the device authorization endpoint.
/// </summary>
public class ValidatedDeviceAuthorizationRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the requested scopes.
    /// </summary>
    /// <value>
    /// The scopes.
    /// </value>
    public IEnumerable<string> RequestedScopes { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is open identifier request.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is open identifier request; otherwise, <c>false</c>.
    /// </value>
    public bool IsOpenIdRequest { get; set; }

    /// <summary>
    /// Gets or sets the description the user assigned to this grant, for example the name of the device or application.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description { get; set; }
}
