namespace MinApiLib.OidcServer.Core.ResourceValidation;

/// <summary>
/// Models the request to validate scopes and resource indicators for a client.
/// </summary>
public class ResourceValidationRequest
{
    /// <summary>
    /// The client.
    /// </summary>
    public Client Client { get; set; } = default!;

    /// <summary>
    /// The requested scope values.
    /// </summary>
    public IEnumerable<string> Scopes { get; set; } = default!;

    /// <summary>
    /// The requested resource indicators (RFC 8707). Each one must name an API resource that one of the requested
    /// scopes belongs to; the access token audience is then limited to those APIs.
    /// </summary>
    public IEnumerable<string>? ResourceIndicators { get; set; }
}
