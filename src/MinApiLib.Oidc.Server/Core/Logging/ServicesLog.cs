namespace MinApiLib.Oidc.Server.Core.Logging;

internal static partial class ServicesLog
{
    // BackChannelLogoutHttpClient
    [LoggerMessage(0x6401, LogLevel.Debug, "Response from back-channel logout endpoint: {url} status code: {status}")]
    public static partial void BackChannelLogoutResponse(ILogger logger, string url, int status);

    [LoggerMessage(0x6402, LogLevel.Warning, "Response from back-channel logout endpoint: {url} status code: {status}")]
    public static partial void BackChannelLogoutResponseFailure(ILogger logger, string url, int status);

    [LoggerMessage(0x6403, LogLevel.Error, "Exception invoking back-channel logout for url: {url}")]
    public static partial void BackChannelLogoutException(ILogger logger, Exception exception, string url);

    // DefaultOidcServerInteractionService / OidcReturnUrlParser
    [LoggerMessage(0x6404, LogLevel.Trace, "AuthorizationRequest being returned")]
    public static partial void AuthorizationRequestReturned(ILogger logger);

    [LoggerMessage(0x6405, LogLevel.Trace, "No AuthorizationRequest being returned")]
    public static partial void NoAuthorizationRequestReturned(ILogger logger);

    [LoggerMessage(0x6406, LogLevel.Trace, "Error context loaded")]
    public static partial void ErrorContextLoaded(ILogger logger);

    [LoggerMessage(0x6407, LogLevel.Trace, "No error context found")]
    public static partial void NoErrorContextFound(ILogger logger);

    [LoggerMessage(0x6408, LogLevel.Trace, "IsValidReturnUrl true")]
    public static partial void IsValidReturnUrlTrue(ILogger logger);

    [LoggerMessage(0x6409, LogLevel.Trace, "IsValidReturnUrl false")]
    public static partial void IsValidReturnUrlFalse(ILogger logger);

    [LoggerMessage(0x640A, LogLevel.Trace, "returnUrl is valid")]
    public static partial void ReturnUrlValid(ILogger logger);

    [LoggerMessage(0x640B, LogLevel.Trace, "returnUrl is not valid")]
    public static partial void ReturnUrlNotValid(ILogger logger);

    // DefaultPersistedGrantService
    [LoggerMessage(0x640C, LogLevel.Error, "Failed processing results from grant store.")]
    public static partial void GrantStoreProcessingFailed(ILogger logger, Exception exception);

    // DefaultClaimsService
    [LoggerMessage(0x640D, LogLevel.Debug, "Getting claims for identity token for subject: {subject} and client: {clientId}")]
    public static partial void GettingIdentityTokenClaims(ILogger logger, string subject, string clientId);

    [LoggerMessage(0x640E, LogLevel.Debug, "In addition to an id_token, an access_token was requested. No claims other than sub are included in the id_token. To obtain more user claims, either use the user info endpoint or set AlwaysIncludeUserClaimsInIdToken on the client configuration.")]
    public static partial void IdTokenUserClaimsOmitted(ILogger logger);

    [LoggerMessage(0x640F, LogLevel.Debug, "Getting claims for access token for client: {clientId}")]
    public static partial void GettingAccessTokenClaimsForClient(ILogger logger, string clientId);

    [LoggerMessage(0x6410, LogLevel.Debug, "Client {clientId} is impersonating {impersonatedClientId}")]
    public static partial void ClientImpersonating(ILogger logger, string clientId, string impersonatedClientId);

    [LoggerMessage(0x6411, LogLevel.Debug, "Getting claims for access token for subject: {subject}")]
    public static partial void GettingAccessTokenClaimsForSubject(ILogger logger, string subject);

    [LoggerMessage(0x6412, LogLevel.Debug, "Claim types from profile service that were filtered: {claimTypes}")]
    public static partial void ProfileClaimTypesFiltered(ILogger logger, IEnumerable<string> claimTypes);

    // DefaultEventSink
    [LoggerMessage(0x6413, LogLevel.Information, "{@event}")]
    public static partial void EventRaised(ILogger logger, object @event);

    // DefaultTokenCreationService
    [LoggerMessage(0x6414, LogLevel.Warning, "Certificate {subjectName} has expired on {expiration}")]
    public static partial void CertificateExpired(ILogger logger, string subjectName, string expiration);

    // DefaultProfileService
    [LoggerMessage(0x6415, LogLevel.Debug, "IsActive called from: {caller}")]
    public static partial void IsActiveCalled(ILogger logger, string caller);

    // DefaultCorsPolicyService
    [LoggerMessage(0x6416, LogLevel.Debug, "AllowAll true, so origin: {origin} is allowed")]
    public static partial void CorsAllowAllOriginAllowed(ILogger logger, string origin);

    [LoggerMessage(0x6417, LogLevel.Debug, "AllowedOrigins configured and origin {origin} is allowed")]
    public static partial void CorsAllowedOriginsOriginAllowed(ILogger logger, string origin);

    [LoggerMessage(0x6418, LogLevel.Debug, "AllowedOrigins configured and origin {origin} is not allowed")]
    public static partial void CorsAllowedOriginsOriginNotAllowed(ILogger logger, string origin);

    [LoggerMessage(0x6419, LogLevel.Debug, "Exiting; origin {origin} is not allowed")]
    public static partial void CorsOriginNotAllowed(ILogger logger, string origin);

    // LogoutNotificationService
    [LoggerMessage(0x641A, LogLevel.Debug, "Client front-channel logout URLs: {urls}")]
    public static partial void FrontChannelLogoutUrls(ILogger logger, string urls);

    [LoggerMessage(0x641B, LogLevel.Debug, "No client front-channel logout URLs")]
    public static partial void NoFrontChannelLogoutUrls(ILogger logger);

    [LoggerMessage(0x641C, LogLevel.Debug, "Client back-channel logout URLs: {urls}")]
    public static partial void BackChannelLogoutUrls(ILogger logger, string urls);

    [LoggerMessage(0x641D, LogLevel.Debug, "No client back-channel logout URLs")]
    public static partial void NoBackChannelLogoutUrls(ILogger logger);

    // DefaultConsentService
    [LoggerMessage(0x641E, LogLevel.Debug, "Client is configured to not require consent, no consent is required")]
    public static partial void ClientDoesNotRequireConsent(ILogger logger);

    [LoggerMessage(0x641F, LogLevel.Debug, "No scopes being requested, no consent is required")]
    public static partial void NoScopesRequestedNoConsentRequired(ILogger logger);

    [LoggerMessage(0x6420, LogLevel.Debug, "Client is configured to not allow remembering consent, consent is required")]
    public static partial void ClientDoesNotAllowRememberConsent(ILogger logger);

    [LoggerMessage(0x6421, LogLevel.Debug, "Scopes contains parameterized values, consent is required")]
    public static partial void ParameterizedScopesConsentRequired(ILogger logger);

    [LoggerMessage(0x6422, LogLevel.Debug, "Scopes contains offline_access, consent is required")]
    public static partial void OfflineAccessConsentRequired(ILogger logger);

    [LoggerMessage(0x6423, LogLevel.Debug, "Found no prior consent from consent store, consent is required")]
    public static partial void NoPriorConsentFound(ILogger logger);

    [LoggerMessage(0x6424, LogLevel.Debug, "Consent found in consent store is expired, consent is required")]
    public static partial void StoredConsentExpired(ILogger logger);

    [LoggerMessage(0x6425, LogLevel.Debug, "Consent found in consent store is different than current request, consent is required")]
    public static partial void StoredConsentDifferent(ILogger logger);

    [LoggerMessage(0x6426, LogLevel.Debug, "Consent found in consent store is same as current request, consent is not required")]
    public static partial void StoredConsentSame(ILogger logger);

    [LoggerMessage(0x6427, LogLevel.Debug, "Consent found in consent store has no scopes, consent is required")]
    public static partial void StoredConsentHasNoScopes(ILogger logger);

    [LoggerMessage(0x6428, LogLevel.Debug, "Client allows remembering consent, and consent given. Updating consent store for subject: {subject}")]
    public static partial void UpdatingConsentStore(ILogger logger, string subject);

    [LoggerMessage(0x6429, LogLevel.Debug, "Client allows remembering consent, and no scopes provided. Removing consent from consent store for subject: {subject}")]
    public static partial void RemovingConsentFromStore(ILogger logger, string subject);

    // DefaultDeviceFlowInteractionService
    [LoggerMessage(0x642A, LogLevel.Warning, "Too many failed user code attempts. Rejecting user code lookups until the window expires.")]
    public static partial void TooManyFailedUserCodeAttempts(ILogger logger);

    [LoggerMessage(0x642B, LogLevel.Error, "{ErrorDescription}")]
    public static partial void DeviceFlowInteractionError(ILogger logger, string errorDescription);

    // DefaultUserSession
    [LoggerMessage(0x642C, LogLevel.Error, "Error decoding client list")]
    public static partial void ClientListDecodeFailed(ILogger logger, Exception exception);

    // InMemoryCorsPolicyService
    [LoggerMessage(0x642D, LogLevel.Debug, "Client list checked and origin: {origin} is allowed")]
    public static partial void ClientListOriginAllowed(ILogger logger, string origin);

    [LoggerMessage(0x642E, LogLevel.Debug, "Client list checked and origin: {origin} is not allowed")]
    public static partial void ClientListOriginNotAllowed(ILogger logger, string origin);

    // AutomaticKeyManager
    [LoggerMessage(0x642F, LogLevel.Information, "Deleting retired signing key {kid}")]
    public static partial void DeletingRetiredSigningKey(ILogger logger, string kid);

    [LoggerMessage(0x6430, LogLevel.Information, "Created signing key {kid} for {algorithm}")]
    public static partial void CreatedSigningKey(ILogger logger, string kid, string algorithm);

    [LoggerMessage(0x6431, LogLevel.Error, "Could not load signing key {kid}. Check that all instances share data protection keys.")]
    public static partial void SigningKeyLoadFailed(ILogger logger, Exception exception, string kid);

    // DefaultTokenService
    [LoggerMessage(0x6432, LogLevel.Trace, "Creating identity token")]
    public static partial void CreatingIdentityToken(ILogger logger);

    [LoggerMessage(0x6433, LogLevel.Trace, "Creating access token")]
    public static partial void CreatingAccessToken(ILogger logger);

    [LoggerMessage(0x6434, LogLevel.Trace, "Creating JWT access token")]
    public static partial void CreatingJwtAccessToken(ILogger logger);

    [LoggerMessage(0x6435, LogLevel.Trace, "Creating reference access token")]
    public static partial void CreatingReferenceAccessToken(ILogger logger);

    [LoggerMessage(0x6436, LogLevel.Trace, "Creating JWT identity token")]
    public static partial void CreatingJwtIdentityToken(ILogger logger);

    // DefaultRefreshTokenService
    [LoggerMessage(0x6437, LogLevel.Trace, "Start refresh token validation")]
    public static partial void StartRefreshTokenValidation(ILogger logger);

    [LoggerMessage(0x6438, LogLevel.Warning, "Invalid refresh token")]
    public static partial void InvalidRefreshToken(ILogger logger);

    [LoggerMessage(0x6439, LogLevel.Warning, "Refresh token has expired.")]
    public static partial void RefreshTokenExpired(ILogger logger);

    [LoggerMessage(0x643A, LogLevel.Error, "{clientId} tries to refresh token belonging to {tokenClientId}")]
    public static partial void RefreshTokenClientMismatch(ILogger logger, string clientId, string tokenClientId);

    [LoggerMessage(0x643B, LogLevel.Error, "{clientId} does not have access to offline_access scope anymore")]
    public static partial void OfflineAccessNoLongerAllowed(ILogger logger, string clientId);

    [LoggerMessage(0x643C, LogLevel.Warning, "Rejecting refresh token because it has been consumed already.")]
    public static partial void RefreshTokenAlreadyConsumed(ILogger logger);

    [LoggerMessage(0x643D, LogLevel.Error, "{subjectId} has been disabled")]
    public static partial void SubjectDisabled(ILogger logger, string subjectId);

    [LoggerMessage(0x643E, LogLevel.Warning, "Refresh token reuse detected for subject {subjectId} and client {clientId}. Revoking their refresh tokens.")]
    public static partial void RefreshTokenReuseDetected(ILogger logger, string? subjectId, string clientId);

    [LoggerMessage(0x643F, LogLevel.Debug, "Creating refresh token")]
    public static partial void CreatingRefreshToken(ILogger logger);

    [LoggerMessage(0x6440, LogLevel.Debug, "Setting an absolute lifetime: {absoluteLifetime}")]
    public static partial void SettingAbsoluteLifetime(ILogger logger, int absoluteLifetime);

    [LoggerMessage(0x6441, LogLevel.Warning, "Client {clientId}'s configured SlidingRefreshTokenLifetime of {slidingLifetime} exceeds its AbsoluteRefreshTokenLifetime of {absoluteLifetime}. The refresh_token's sliding lifetime will be capped to the absolute lifetime")]
    public static partial void SlidingLifetimeExceedsAbsoluteLifetime(ILogger logger, string clientId, int slidingLifetime, int absoluteLifetime);

    [LoggerMessage(0x6442, LogLevel.Debug, "Setting a sliding lifetime: {slidingLifetime}")]
    public static partial void SettingSlidingLifetime(ILogger logger, int slidingLifetime);

    [LoggerMessage(0x6443, LogLevel.Debug, "Updating refresh token")]
    public static partial void UpdatingRefreshToken(ILogger logger);

    [LoggerMessage(0x6444, LogLevel.Debug, "Token usage is one-time only. Setting current handle as consumed, and generating new handle")]
    public static partial void OneTimeOnlyTokenUsage(ILogger logger);

    [LoggerMessage(0x6445, LogLevel.Debug, "Refresh token expiration is sliding - extending lifetime")]
    public static partial void ExtendingSlidingLifetime(ILogger logger);

    [LoggerMessage(0x6446, LogLevel.Debug, "Current lifetime: {currentLifetime}")]
    public static partial void CurrentLifetime(ILogger logger, int currentLifetime);

    [LoggerMessage(0x6447, LogLevel.Debug, "New lifetime: {slidingLifetime}")]
    public static partial void NewLifetime(ILogger logger, int slidingLifetime);

    [LoggerMessage(0x6448, LogLevel.Debug, "New lifetime exceeds absolute lifetime, capping it to {newLifetime}")]
    public static partial void NewLifetimeCapped(ILogger logger, int newLifetime);

    [LoggerMessage(0x6449, LogLevel.Debug, "Created refresh token in store")]
    public static partial void CreatedRefreshTokenInStore(ILogger logger);

    [LoggerMessage(0x644A, LogLevel.Debug, "Updated refresh token in store")]
    public static partial void UpdatedRefreshTokenInStore(ILogger logger);

    [LoggerMessage(0x644B, LogLevel.Debug, "No updates to refresh token done")]
    public static partial void NoRefreshTokenUpdates(ILogger logger);
}
