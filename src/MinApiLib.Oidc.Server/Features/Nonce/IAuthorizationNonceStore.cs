namespace MinApiLib.Oidc.Server.Features.Nonce;

public interface IAuthorizationNonceStore
{
    /// <summary>
    /// Stores the nonce token.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    Task<string> StoreAuthorizationNonceAsync(AuthorizationNonce nonce);

    /// <summary>
    /// Gets the nonce token.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    Task<AuthorizationNonce?> GetAuthorizationNonceAsync(string nonce);

    /// <summary>
    /// Removes the nonce token.
    /// </summary>
    /// <param name="nonce">The nonce.</param>
    /// <returns></returns>
    Task RemoveAuthorizationNonceAsync(string nonce);
}
