namespace MinApiLib.Oidc.Server.Core.Stores.Serialization;

public class ClaimsPrincipalLite
{
    public string? AuthenticationType { get; set; }
    public ClaimLite[]? Claims { get; set; }
}
