namespace MinApiLib.OidcServer.Stores.Serialization;

public class ClaimsPrincipalLite
{
    public string? AuthenticationType { get; set; }
    public ClaimLite[]? Claims { get; set; }
}
