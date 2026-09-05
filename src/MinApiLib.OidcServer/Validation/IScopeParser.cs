namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Allows parsing raw scopes values into structured scope values.
/// </summary>
public interface IScopeParser
{
    /// <summary>
    /// Parses the requested scopes.
    /// </summary>
    ParsedScopesResult ParseScopeValues(IEnumerable<string> scopeValues);
}
