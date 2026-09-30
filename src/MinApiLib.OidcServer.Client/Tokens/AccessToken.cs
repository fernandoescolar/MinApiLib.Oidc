namespace MinApiLib.OidcServer.Client.Tokens;

/// <summary>
/// An access token and how to send it.
/// </summary>
/// <param name="Value">The token.</param>
/// <param name="TokenType">The <c>token_type</c> the token endpoint answered: <c>Bearer</c> or <c>DPoP</c>.</param>
/// <param name="ExpiresAt">When the token expires, if the token endpoint said so.</param>
/// <param name="DPoPKey">The key the token is bound to, for DPoP tokens.</param>
public sealed record AccessToken(string Value, string TokenType, DateTimeOffset? ExpiresAt, DPoPKey? DPoPKey)
{
    /// <summary>The token type of DPoP-bound tokens.</summary>
    public const string DPoPTokenType = "DPoP";

    /// <summary>The token type of bearer tokens.</summary>
    public const string BearerTokenType = "Bearer";

    /// <summary>
    /// Gets whether the token is DPoP-bound: it is sent with the <c>DPoP</c> scheme and a proof of <see cref="DPoPKey"/>.
    /// </summary>
    public bool IsDPoP => DPoPKey != null && string.Equals(TokenType, DPoPTokenType, StringComparison.OrdinalIgnoreCase);

    /// <summary>The <c>Authorization</c> scheme to send the token with.</summary>
    public string Scheme => IsDPoP ? DPoPTokenType : BearerTokenType;

    internal bool IsValid(DateTimeOffset now, TimeSpan margin) => ExpiresAt == null || ExpiresAt.Value - margin > now;
}
