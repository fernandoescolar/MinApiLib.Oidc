using MinApiLib.OidcServer.Client.Http;

namespace MinApiLib.OidcServer.Client.OpenIdConnect;

/// <summary>
/// Sends the signed-in user's access token with each request.
/// </summary>
internal sealed class UserAccessTokenHandler(
    string? signInScheme,
    IHttpContextAccessor httpContextAccessor,
    IUserAccessTokenManager tokens,
    DPoPNonceStore nonces,
    TimeProvider time,
    ILogger<UserAccessTokenHandler> logger) : AccessTokenHandler(nonces, time, logger)
{
    protected override async Task<AccessToken> GetAccessTokenAsync(bool forceRenewal, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new AccessTokenException("There is no current HTTP request: the user's access token can only be sent while handling one.");

        return await tokens.GetAccessTokenAsync(httpContext, signInScheme, forceRenewal, cancellationToken)
            ?? throw new AccessTokenException("There is no signed-in user with an access token. Sign the user in with an OpenID Connect scheme registered with AddOpenIdConnectTokens.");
    }
}
