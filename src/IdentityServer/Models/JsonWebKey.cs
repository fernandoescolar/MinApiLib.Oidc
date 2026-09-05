namespace IdentityServer.Models;

public class JsonWebKey
{
    public string kty { get; set; } = default!;
    public string use { get; set; } = default!;
    public string kid { get; set; } = default!;
    public string? x5t { get; set; }
    public string? e { get; set; }
    public string? n { get; set; }
    public string[]? x5c { get; set; }
    public string alg { get; set; } = default!;

    public string? x { get; set; }
    public string? y { get; set; }
    public string? crv { get; set; }
}
