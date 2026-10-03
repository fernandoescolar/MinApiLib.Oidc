namespace MinApiLib.Oidc.Http.ClientCredentials;

/// <summary>
/// Sends the access token of a client credentials client with each request.
/// </summary>
internal sealed class ClientCredentialsTokenHandler(
    string clientName,
    IClientCredentialsTokenManager tokens,
    DPoPNonceStore nonces,
    TimeProvider time,
    ILogger<ClientCredentialsTokenHandler> logger) : AccessTokenHandler(nonces, time, logger)
{
    protected override Task<AccessToken> GetAccessTokenAsync(bool forceRenewal, CancellationToken cancellationToken)
        => tokens.GetAccessTokenAsync(clientName, forceRenewal, cancellationToken);
}
