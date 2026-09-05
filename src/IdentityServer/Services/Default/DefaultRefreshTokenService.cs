namespace IdentityServer.Services;

/// <summary>
/// Default refresh token service
/// </summary>
public class DefaultRefreshTokenService : IRefreshTokenService
{
    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The refresh token store
    /// </summary>
    protected IRefreshTokenStore RefreshTokenStore { get; }

    /// <summary>
    /// The profile service
    /// </summary>
    protected IProfileService Profile { get; }

    /// <summary>
    /// The clock
    /// </summary>
    protected TimeProvider Clock { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
    /// </summary>
    /// <param name="refreshTokenStore">The refresh token store</param>
    /// <param name="profile"></param>
    /// <param name="clock">The clock</param>
    /// <param name="logger">The logger</param>
    public DefaultRefreshTokenService(IRefreshTokenStore refreshTokenStore, IProfileService profile,
        TimeProvider clock,
        ILogger<DefaultRefreshTokenService> logger)
    {
        RefreshTokenStore = refreshTokenStore;
        Profile = profile;
        Clock = clock;

        Logger = logger;
    }

    /// <summary>
    /// Validates a refresh token
    /// </summary>
    /// <param name="token">The token handle.</param>
    /// <param name="client">The client.</param>
    /// <returns></returns>
    public virtual async Task<TokenValidationResult> ValidateRefreshTokenAsync(string token, Client client)
    {
        var invalidGrant = new TokenValidationResult
        {
            IsError = true,
            Error = OidcConstants.TokenErrors.InvalidGrant
        };

        ServicesLog.StartRefreshTokenValidation(Logger);

        var refreshToken = await RefreshTokenStore.GetRefreshTokenAsync(token);
        if (refreshToken == null)
        {
            ServicesLog.InvalidRefreshToken(Logger);
            return invalidGrant;
        }

        if (refreshToken.CreationTime.HasExceeded(refreshToken.Lifetime, Clock.GetUtcNow().DateTime))
        {
            ServicesLog.RefreshTokenExpired(Logger);
            return invalidGrant;
        }

        if (client.ClientId != refreshToken.ClientId)
        {
            ServicesLog.RefreshTokenClientMismatch(Logger, client.ClientId, refreshToken.ClientId);
            return invalidGrant;
        }

        if (!client.AllowOfflineAccess)
        {
            ServicesLog.OfflineAccessNoLongerAllowed(Logger, client.ClientId);
            return invalidGrant;
        }

        if (refreshToken.ConsumedTime.HasValue)
        {
            if ((await AcceptConsumedTokenAsync(refreshToken)) == false)
            {
                ServicesLog.RefreshTokenAlreadyConsumed(Logger);
                await OnConsumedTokenReusedAsync(refreshToken);
                return invalidGrant;
            }
        }

        var isActiveCtx = new IsActiveContext(
            refreshToken.Subject,
            client,
            IdentityServerConstants.ProfileIsActiveCallers.RefreshTokenValidation);

        await Profile.IsActiveAsync(isActiveCtx);

        if (isActiveCtx.IsActive == false)
        {
            ServicesLog.SubjectDisabled(Logger, refreshToken.Subject.GetSubjectId());
            return invalidGrant;
        }

        return new TokenValidationResult
        {
            IsError = false,
            RefreshToken = refreshToken,
            Client = client
        };
    }

    /// <summary>
    /// Callback to decide if an already consumed token should be accepted.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    protected virtual Task<bool> AcceptConsumedTokenAsync(RefreshToken refreshToken)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Called when a rotated (already consumed) refresh token is presented again and rejected.
    /// Reuse indicates that either the client or an attacker holds a stolen token (RFC 9700 section 4.14.2),
    /// so by default every refresh token of the same subject and client is revoked. Override to change this,
    /// for example to tolerate client retries together with <see cref="AcceptConsumedTokenAsync"/>.
    /// </summary>
    /// <param name="refreshToken">The consumed refresh token that was presented.</param>
    /// <returns></returns>
    protected virtual Task OnConsumedTokenReusedAsync(RefreshToken refreshToken)
    {
        ServicesLog.RefreshTokenReuseDetected(Logger, refreshToken.SubjectId, refreshToken.ClientId);

        // Refresh tokens are only issued for grants with a user.
        var subjectId = refreshToken.SubjectId ?? throw new InvalidOperationException("The refresh token has no subject.");
        return RefreshTokenStore.RemoveRefreshTokensAsync(subjectId, refreshToken.ClientId);
    }

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
    public virtual async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken,
        Client client, IEnumerable<string>? authorizedResourceIndicators = null)
    {
        ServicesLog.CreatingRefreshToken(Logger);

        int lifetime;
        if (client.RefreshTokenExpiration == TokenExpiration.Absolute)
        {
            ServicesLog.SettingAbsoluteLifetime(Logger, client.AbsoluteRefreshTokenLifetime);
            lifetime = client.AbsoluteRefreshTokenLifetime;
        }
        else
        {
            lifetime = client.SlidingRefreshTokenLifetime;
            if (client.AbsoluteRefreshTokenLifetime > 0 && lifetime > client.AbsoluteRefreshTokenLifetime)
            {
                ServicesLog.SlidingLifetimeExceedsAbsoluteLifetime(Logger, client.ClientId, lifetime, client.AbsoluteRefreshTokenLifetime);
                lifetime = client.AbsoluteRefreshTokenLifetime;
            }

            ServicesLog.SettingSlidingLifetime(Logger, lifetime);
        }

        var refreshToken = new RefreshToken
        {
            CreationTime = Clock.GetUtcNow().UtcDateTime,
            Lifetime = lifetime,
            AccessToken = accessToken,
            AuthorizedResourceIndicators = authorizedResourceIndicators?.ToArray()
        };

        var handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
        return handle;
    }

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
    public virtual async Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken,
        Client client, Token? newAccessToken = null)
    {
        ServicesLog.UpdatingRefreshToken(Logger);

        bool needsCreate = false;
        bool needsUpdate = false;

        if (client.RefreshTokenUsage == TokenUsage.OneTimeOnly)
        {
            ServicesLog.OneTimeOnlyTokenUsage(Logger);

            if (refreshToken.ConsumedTime == null)
            {
                refreshToken.ConsumedTime = Clock.GetUtcNow().UtcDateTime;
                await RefreshTokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
            }

            needsCreate = true;
        }

        if (client.RefreshTokenExpiration == TokenExpiration.Sliding)
        {
            ServicesLog.ExtendingSlidingLifetime(Logger);

            var currentLifetime = refreshToken.CreationTime.GetLifetimeInSeconds(Clock.GetUtcNow().UtcDateTime);
            ServicesLog.CurrentLifetime(Logger, currentLifetime);

            var newLifetime = currentLifetime + client.SlidingRefreshTokenLifetime;
            ServicesLog.NewLifetime(Logger, newLifetime);

            if (client.AbsoluteRefreshTokenLifetime > 0 && newLifetime > client.AbsoluteRefreshTokenLifetime)
            {
                newLifetime = client.AbsoluteRefreshTokenLifetime;
                ServicesLog.NewLifetimeCapped(Logger, newLifetime);
            }

            refreshToken.Lifetime = newLifetime;
            needsUpdate = true;
        }

        if (needsCreate)
        {
            refreshToken.ConsumedTime = null;
            if (newAccessToken != null)
                refreshToken.AccessToken = newAccessToken;
            handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
            ServicesLog.CreatedRefreshTokenInStore(Logger);
        }
        else if (needsUpdate)
        {
            if (newAccessToken != null)
                refreshToken.AccessToken = newAccessToken;

            await RefreshTokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
            ServicesLog.UpdatedRefreshTokenInStore(Logger);
        }
        else
        {
            ServicesLog.NoRefreshTokenUpdates(Logger);
        }

        return handle;
    }
}
