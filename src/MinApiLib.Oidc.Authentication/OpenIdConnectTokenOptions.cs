namespace MinApiLib.Oidc.Authentication;

/// <summary>
/// How an OpenID Connect scheme gets, keeps and refreshes the signed-in user's tokens.
/// </summary>
public sealed class OpenIdConnectTokenOptions
{
    /// <summary>
    /// Gets or sets whether to ask for DPoP-bound tokens. Each sign-in creates a key, kept protected in the session;
    /// the code redemption, the refreshes and the API calls carry proofs of it. Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseDPoP { get; set; }

    /// <summary>
    /// Gets or sets whether to send the thumbprint of the key in the authorization request (<c>dpop_jkt</c>, RFC 9449
    /// section 10), which binds the authorization code to the key: a stolen code cannot be redeemed with another key.
    /// Defaults to <see langword="true"/>. Only used with <see cref="UseDPoP"/>.
    /// </summary>
    public bool BindAuthorizationCode { get; set; } = true;

    /// <summary>
    /// Gets or sets the function that creates the key of each sign-in. Defaults to <see cref="DPoPKey.CreateECDsa"/>.
    /// </summary>
    public Func<DPoPKey> CreateDPoPKey { get; set; } = DPoPKey.CreateECDsa;

    /// <summary>
    /// Gets or sets how long before it expires the access token is refreshed with the refresh token. Defaults to
    /// 1 minute. Ask for the <c>offline_access</c> scope to get a refresh token.
    /// </summary>
    public TimeSpan RefreshBeforeExpiration { get; set; } = TimeSpan.FromMinutes(1);
}
