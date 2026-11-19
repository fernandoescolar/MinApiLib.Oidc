namespace IdP.Features.IdentityProviders;

/// <summary>
/// An external OpenID Connect provider users can sign in with (identity brokering). The alias is the authentication
/// scheme and the <c>idp</c> claim of the users who sign in through it.
/// </summary>
public sealed class IdentityProviderDocument
{
    /// <summary>The alias: letters, digits, <c>-</c> and <c>_</c>. It names the scheme and the callback path.</summary>
    [BsonId]
    public string Alias { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public bool Enabled { get; set; } = true;

    /// <summary>The issuer: its discovery document is <c>{Authority}/.well-known/openid-configuration</c>.</summary>
    public string Authority { get; set; } = default!;

    public string ClientId { get; set; } = default!;

    /// <summary>The client secret, protected with data protection. Empty for public clients (PKCE only).</summary>
    public string? ClientSecret { get; set; }

    public List<string> Scopes { get; set; } = ["openid", "profile", "email"];

    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    public bool UsePkce { get; set; } = true;

    /// <summary>Create a local account for a user who signs in for the first time. Otherwise only linked accounts can sign in.</summary>
    public bool AutoProvision { get; set; } = true;

    /// <summary>Trust the provider's e-mail: mark it confirmed, and link new sign-ins to an existing account with the same e-mail.</summary>
    public bool TrustEmail { get; set; }

    public bool ShowOnLoginPage { get; set; } = true;

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string CallbackPath => $"/external/signin-{Alias}";

    public string SignedOutCallbackPath => $"/external/signout-{Alias}";

    public static bool IsValidAlias(string? alias)
        => !string.IsNullOrEmpty(alias) && alias.Length <= 64 && alias.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_')
            && !string.Equals(alias, SignIn.LocalIdentityProvider, StringComparison.OrdinalIgnoreCase);
}
