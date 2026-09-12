namespace MinApiLib.OidcServer.Core.Stores;

/// <summary>
/// Interface for the authorization code store
/// </summary>
public interface IAuthorizationCodeStore
{
    /// <summary>
    /// Stores the authorization code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code);

    /// <summary>
    /// Gets the authorization code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    Task<AuthorizationCode?> GetAuthorizationCodeAsync(string code);

    /// <summary>
    /// Removes the authorization code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    Task RemoveAuthorizationCodeAsync(string code);

    /// <summary>
    /// Marks the authorization code as consumed (its <see cref="AuthorizationCode.ConsumedTime"/> is set by the caller).
    /// Stores that keep consumed codes until they expire enable replay detection. The default implementation
    /// removes the code, which keeps single use but cannot detect replays.
    /// </summary>
    /// <param name="code">The code handle.</param>
    /// <param name="authorizationCode">The code, with <see cref="AuthorizationCode.ConsumedTime"/> set.</param>
    /// <returns></returns>
    Task ConsumeAuthorizationCodeAsync(string code, AuthorizationCode authorizationCode) => RemoveAuthorizationCodeAsync(code);
}
