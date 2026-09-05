namespace MinApiLib.OidcServer.ResponseHandling;

public class DeviceAuthorizationResponse
{
    public string DeviceCode { get; set; } = default!;
    public string UserCode { get; set; } = default!;
    public string VerificationUri { get; set; } = default!;

    public string? VerificationUriComplete { get; set; }
    public int DeviceCodeLifetime { get; set; }
    public int Interval { get; set; }
}
