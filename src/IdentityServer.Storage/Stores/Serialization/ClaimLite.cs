namespace IdentityServer.Stores.Serialization;

public class ClaimLite
{
    public string Type { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? ValueType { get; set; }
}
