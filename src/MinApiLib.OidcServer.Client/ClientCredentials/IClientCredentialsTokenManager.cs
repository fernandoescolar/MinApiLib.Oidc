namespace MinApiLib.OidcServer.Client.ClientCredentials;

/// <summary>
/// Gets and caches the access tokens of the clients registered with <c>AddClientCredentialsTokens</c>.
/// </summary>
public interface IClientCredentialsTokenManager
{
    /// <summary>
    /// Gets the client's access token: the cached one while it is valid, a new one otherwise.
    /// </summary>
    /// <param name="clientName">The name the client was registered with.</param>
    /// <param name="forceRenewal">Ask for a new token even if the cached one has not expired.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="AccessTokenException">The token endpoint answered an error.</exception>
    Task<AccessToken> GetAccessTokenAsync(string clientName, bool forceRenewal = false, CancellationToken cancellationToken = default);
}
