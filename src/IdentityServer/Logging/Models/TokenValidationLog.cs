namespace IdentityServer.Logging.Models;

internal class TokenValidationLog
{
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public bool ValidateLifetime { get; set; }

    public string? AccessTokenType { get; set; }
    public string? ExpectedScope { get; set; }
    public string? TokenHandle { get; set; }
    public string? JwtId { get; set; }

    public Dictionary<string, object>? Claims { get; set; }

    public override string ToString()
    {
        return LogSerializer.Serialize(this);
    }
}
