namespace IdentityServer.Services;

/// <summary>
/// Resolves the issuer identifier that tokens, discovery and validation use.
/// </summary>
public interface IIssuerNameService
{
    /// <summary>
    /// Returns the issuer identifier for the current context.
    /// </summary>
    /// <returns>The issuer identifier, without a trailing slash.</returns>
    Task<string> GetCurrentAsync();
}
