namespace MinApiLib.OidcServer.Client.OpenIdConnect;

/// <summary>
/// Gets the signed-in user's access token from the session, and refreshes it shortly before it expires.
/// </summary>
public interface IUserAccessTokenManager
{
    /// <summary>
    /// Gets the user's access token, refreshed when it is about to expire. A refresh also updates the session cookie,
    /// while the response has not started.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <param name="signInScheme">The scheme that keeps the session, usually the cookie scheme. Defaults to the default authenticate scheme.</param>
    /// <param name="forceRefresh">Refresh the token even if it has not expired.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The token, or <see langword="null"/> when there is no signed-in user or the session has no access token.</returns>
    /// <exception cref="AccessTokenException">The refresh failed, for example because the refresh token expired.</exception>
    Task<AccessToken?> GetAccessTokenAsync(HttpContext httpContext, string? signInScheme = null, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
