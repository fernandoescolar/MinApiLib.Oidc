namespace IdentityServer.Services;

/// <summary>
/// Implements refresh token creation and validation
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Validates a refresh token.
    /// </summary>
    /// <param name="token">The refresh token.</param>
    /// <param name="client">The client.</param>
    /// <returns></returns>
    Task<TokenValidationResult> ValidateRefreshTokenAsync(string token, Client client);

    /// <summary>
    /// Creates the refresh token.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="client">The client.</param>
    /// <param name="authorizedResourceIndicators">The resource indicators (RFC 8707) the grant was authorized for.</param>
    /// <returns>
    /// The refresh token handle
    /// </returns>
    Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken, Client client, IEnumerable<string>? authorizedResourceIndicators = null);

    /// <summary>
    /// Updates the refresh token.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="client">The client.</param>
    /// <param name="newAccessToken">New access token to store with refresh token</param>
    /// <returns>
    /// The refresh token handle
    /// </returns>
    Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, Client client, Token? newAccessToken = null);
}
