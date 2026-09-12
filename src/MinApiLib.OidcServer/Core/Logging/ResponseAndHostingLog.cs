namespace MinApiLib.OidcServer.Core.Logging;

internal static partial class ResponseAndHostingLog
{
    // TokenResponseGenerator

    [LoggerMessage(0x6600, LogLevel.Trace, "Creating response for client credentials request")]
    public static partial void CreatingClientCredentialsResponse(ILogger logger);

    [LoggerMessage(0x6601, LogLevel.Trace, "Creating response for authorization code request")]
    public static partial void CreatingAuthorizationCodeResponse(ILogger logger);

    [LoggerMessage(0x6602, LogLevel.Trace, "Creating response for refresh token request")]
    public static partial void CreatingRefreshTokenResponse(ILogger logger);

    [LoggerMessage(0x6603, LogLevel.Trace, "Creating response for device code request")]
    public static partial void CreatingDeviceCodeResponse(ILogger logger);

    [LoggerMessage(0x6604, LogLevel.Trace, "Creating response for extension grant request")]
    public static partial void CreatingExtensionGrantResponse(ILogger logger);

    // AuthorizeResponseGenerator

    [LoggerMessage(0x6605, LogLevel.Error, "Unsupported grant type: {GrantType}")]
    public static partial void UnsupportedGrantType(ILogger logger, string grantType);

    [LoggerMessage(0x6606, LogLevel.Debug, "Creating Hybrid Flow response.")]
    public static partial void CreatingHybridFlowResponse(ILogger logger);

    [LoggerMessage(0x66A2, LogLevel.Debug, "Creating Implicit Flow response.")]
    public static partial void CreatingImplicitFlowResponse(ILogger logger);

    [LoggerMessage(0x66A3, LogLevel.Trace, "Creating response for password request")]
    public static partial void CreatingPasswordResponse(ILogger logger);

    [LoggerMessage(0x6607, LogLevel.Debug, "Creating Authorization Code Flow response.")]
    public static partial void CreatingAuthorizationCodeFlowResponse(ILogger logger);

    // TokenRevocationResponseGenerator

    [LoggerMessage(0x6608, LogLevel.Trace, "Hint was for access token")]
    public static partial void RevocationHintAccessToken(ILogger logger);

    [LoggerMessage(0x6609, LogLevel.Trace, "Hint was for refresh token")]
    public static partial void RevocationHintRefreshToken(ILogger logger);

    [LoggerMessage(0x660A, LogLevel.Trace, "No hint for token type")]
    public static partial void RevocationNoHint(ILogger logger);

    [LoggerMessage(0x660B, LogLevel.Debug, "Access token revoked")]
    public static partial void AccessTokenRevoked(ILogger logger);

    [LoggerMessage(0x660C, LogLevel.Warning, "Client {clientId} denied from revoking access token belonging to Client {tokenClientId}")]
    public static partial void AccessTokenRevocationDenied(ILogger logger, string clientId, string tokenClientId);

    [LoggerMessage(0x660D, LogLevel.Debug, "Refresh token revoked")]
    public static partial void RefreshTokenRevoked(ILogger logger);

    [LoggerMessage(0x660E, LogLevel.Warning, "Client {clientId} denied from revoking a refresh token belonging to Client {tokenClientId}")]
    public static partial void RefreshTokenRevocationDenied(ILogger logger, string clientId, string tokenClientId);

    // DeviceAuthorizationResponseGenerator

    [LoggerMessage(0x660F, LogLevel.Trace, "Creating response for device authorization request")]
    public static partial void CreatingDeviceAuthorizationResponse(ILogger logger);

    // UserInfoResponseGenerator

    [LoggerMessage(0x6610, LogLevel.Debug, "Creating userinfo response")]
    public static partial void CreatingUserInfoResponse(ILogger logger);

    [LoggerMessage(0x6611, LogLevel.Debug, "Requested claim types: {claimTypes}")]
    public static partial void RequestedClaimTypes(ILogger logger, string claimTypes);

    [LoggerMessage(0x6612, LogLevel.Information, "Profile service returned no claims (null)")]
    public static partial void ProfileServiceReturnedNoClaims(ILogger logger);

    [LoggerMessage(0x6613, LogLevel.Information, "Profile service returned the following claim types: {types}")]
    public static partial void ProfileServiceReturnedClaimTypes(ILogger logger, string types);

    [LoggerMessage(0x6614, LogLevel.Error, "Profile service returned incorrect subject value: {sub}")]
    public static partial void ProfileServiceReturnedIncorrectSubject(ILogger logger, Claim sub);

    [LoggerMessage(0x6615, LogLevel.Debug, "Scopes in access token: {scopes}")]
    public static partial void ScopesInAccessToken(ILogger logger, string scopes);

    // CorsPolicyProvider

    [LoggerMessage(0x6616, LogLevel.Debug, "CORS request made for path: {path} from origin: {origin}")]
    public static partial void CorsRequest(ILogger logger, PathString path, string origin);

    [LoggerMessage(0x6617, LogLevel.Debug, "CorsPolicyService allowed origin: {origin}")]
    public static partial void CorsOriginAllowed(ILogger logger, string origin);

    [LoggerMessage(0x6618, LogLevel.Warning, "CorsPolicyService did not allow origin: {origin}")]
    public static partial void CorsOriginNotAllowed(ILogger logger, string origin);

    [LoggerMessage(0x6619, LogLevel.Debug, "CORS request made for path: {path} from origin: {origin} but was ignored because path was not for an allowed OidcServer CORS endpoint")]
    public static partial void CorsRequestIgnored(ILogger logger, PathString path, string origin);

    // OidcServerAuthenticationService

    [LoggerMessage(0x661A, LogLevel.Debug, "Augmenting SignInContext")]
    public static partial void AugmentingSignInContext(ILogger logger);

    [LoggerMessage(0x661B, LogLevel.Debug, "Removing amr claim with value: {value}")]
    public static partial void RemovingAmrClaim(ILogger logger, string value);

    [LoggerMessage(0x661C, LogLevel.Debug, "Adding idp claim with value: {value}")]
    public static partial void AddingIdpClaim(ILogger logger, string value);

    [LoggerMessage(0x661D, LogLevel.Debug, "Adding amr claim with value: {value}")]
    public static partial void AddingAmrClaim(ILogger logger, string value);

    [LoggerMessage(0x661E, LogLevel.Debug, "Adding auth_time claim with value: {value}")]
    public static partial void AddingAuthTimeClaim(ILogger logger, string value);

    // OidcServerEndpoint

    [LoggerMessage(0x661F, LogLevel.Information, "Invoking OidcServer endpoint: {endpointType} for {url}")]
    public static partial void InvokingEndpoint(ILogger logger, string endpointType, string url);

    [LoggerMessage(0x6620, LogLevel.Trace, "Invoking result: {type}")]
    public static partial void InvokingResult(ILogger logger, string type);

    [LoggerMessage(0x6621, LogLevel.Critical, "Unhandled exception: {message}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string message);

    // MutualTlsEndpointMiddleware

    [LoggerMessage(0x6623, LogLevel.Debug, "MTLS authentication failed, error: {error}.")]
    public static partial void MtlsAuthenticationFailed(ILogger logger, string? error);

    // AuthorizeInteractionResponseGenerator

    [LoggerMessage(0x6624, LogLevel.Trace, "ProcessInteractionAsync")]
    public static partial void ProcessInteraction(ILogger logger);

    [LoggerMessage(0x6625, LogLevel.Information, "Interaction error: User consent result: {error}")]
    public static partial void UserConsentResultError(ILogger logger, AuthorizationError? error);

    [LoggerMessage(0x6626, LogLevel.Information, "Changing response to LoginRequired: prompt=none was requested")]
    public static partial void ChangingResponseToLoginRequired(ILogger logger);

    [LoggerMessage(0x6627, LogLevel.Information, "Interaction error: prompt is not allowed in nonce pattern flow.")]
    public static partial void PromptNotAllowedInNonceFlow(ILogger logger);

    [LoggerMessage(0x6628, LogLevel.Information, "Interaction error: nonce is missing.")]
    public static partial void NonceMissing(ILogger logger);

    [LoggerMessage(0x6629, LogLevel.Information, "Interaction error: nonce is invalid.")]
    public static partial void NonceInvalid(ILogger logger);

    [LoggerMessage(0x662A, LogLevel.Information, "Interaction error: nonce has expired.")]
    public static partial void NonceExpired(ILogger logger);

    [LoggerMessage(0x662B, LogLevel.Information, "Interaction error: User is not active.")]
    public static partial void NonceUserNotActive(ILogger logger);

    [LoggerMessage(0x662C, LogLevel.Information, "Interaction error: nonce subject is missing.")]
    public static partial void NonceSubjectMissing(ILogger logger);

    [LoggerMessage(0x662D, LogLevel.Information, "User is already authenticated with the subject from the nonce: {subject}.")]
    public static partial void UserAlreadyAuthenticatedWithNonceSubject(ILogger logger, string subject);

    [LoggerMessage(0x662E, LogLevel.Information, "User is authenticated with a different subject: {subject}. Signing out.")]
    public static partial void UserAuthenticatedWithDifferentSubject(ILogger logger, string? subject);

    [LoggerMessage(0x662F, LogLevel.Information, "Signing in with the subject from the nonce: {subject}.")]
    public static partial void SigningInWithNonceSubject(ILogger logger, string subject);

    [LoggerMessage(0x6630, LogLevel.Information, "Showing login: request contains prompt={prompt}")]
    public static partial void ShowingLoginPrompt(ILogger logger, string prompt);

    [LoggerMessage(0x6631, LogLevel.Information, "Showing login: User is not authenticated")]
    public static partial void ShowingLoginNotAuthenticated(ILogger logger);

    [LoggerMessage(0x6632, LogLevel.Information, "Showing login: User is not active")]
    public static partial void ShowingLoginNotActive(ILogger logger);

    [LoggerMessage(0x6633, LogLevel.Information, "Showing login: Current IdP ({currentIdp}) is not the requested IdP ({idp})")]
    public static partial void ShowingLoginIdpMismatch(ILogger logger, string currentIdp, string idp);

    [LoggerMessage(0x6634, LogLevel.Information, "Showing login: Requested MaxAge exceeded.")]
    public static partial void ShowingLoginMaxAgeExceeded(ILogger logger);

    [LoggerMessage(0x6635, LogLevel.Information, "Showing login: User logged in locally, but client does not allow local logins")]
    public static partial void ShowingLoginLocalLoginNotAllowed(ILogger logger);

    [LoggerMessage(0x6636, LogLevel.Information, "Showing login: User is logged in with idp: {idp}, but idp not in client restriction list.")]
    public static partial void ShowingLoginIdpRestricted(ILogger logger, string idp);

    [LoggerMessage(0x6637, LogLevel.Information, "Showing login: User's auth session duration: {sessionDuration} exceeds client's user SSO lifetime: {userSsoLifetime}.")]
    public static partial void ShowingLoginSsoLifetimeExceeded(ILogger logger, long sessionDuration, int? userSsoLifetime);

    [LoggerMessage(0x6638, LogLevel.Error, "Invalid prompt mode: {promptMode}")]
    public static partial void InvalidPromptMode(ILogger logger, string promptMode);

    [LoggerMessage(0x66A0, LogLevel.Warning, "Prompt value {prompt} is accepted but no IAuthorizePromptHandler is registered for it; ignoring it.")]
    public static partial void NoPromptHandler(ILogger logger, string prompt);

    [LoggerMessage(0x66A1, LogLevel.Debug, "Custom prompt {prompt} handled; removing prompt from the request.")]
    public static partial void CustomPromptHandled(ILogger logger, string prompt);

    [LoggerMessage(0x6639, LogLevel.Information, "Interaction error: prompt=none requested, but consent is required.")]
    public static partial void ConsentRequiredWithPromptNone(ILogger logger);

    [LoggerMessage(0x663A, LogLevel.Information, "Showing consent: User has not yet consented")]
    public static partial void ShowingConsent(ILogger logger);

    [LoggerMessage(0x663B, LogLevel.Trace, "Consent was shown to user")]
    public static partial void ConsentWasShown(ILogger logger);

    [LoggerMessage(0x663C, LogLevel.Information, "Interaction error: User denied consent to required scopes")]
    public static partial void UserDeniedRequiredScopes(ILogger logger);

    [LoggerMessage(0x663D, LogLevel.Information, "User consented to scopes: {scopes}")]
    public static partial void UserConsentedToScopes(ILogger logger, IEnumerable<string> scopes);

    [LoggerMessage(0x663E, LogLevel.Debug, "User indicated to remember consent for scopes: {scopes}")]
    public static partial void UserRememberedConsent(ILogger logger, IEnumerable<string> scopes);

    // BackChannelLogoutBackgroundService

    [LoggerMessage(0x663F, LogLevel.Error, "Sending back-channel logout notifications for session {sessionId} failed.")]
    public static partial void BackChannelLogoutFailed(ILogger logger, Exception exception, string? sessionId);

    // OidcServerMiddleware

    [LoggerMessage(0x6640, LogLevel.Debug, "SignOutCalled set; processing post-signout session cleanup.")]
    public static partial void ProcessingPostSignOutCleanup(ILogger logger);

    [LoggerMessage(0x6641, LogLevel.Debug, "Back-channel logout notifications queued for session {sessionId}.")]
    public static partial void BackChannelLogoutQueued(ILogger logger, string? sessionId);

    // DiscoveryResponseGenerator

    [LoggerMessage(0x6642, LogLevel.Error, "Discovery custom entry {key} cannot be added, because it already exists.")]
    public static partial void DiscoveryCustomEntryExists(ILogger logger, string key);

    // IntrospectionResponseGenerator

    [LoggerMessage(0x6648, LogLevel.Trace, "Creating introspection response")]
    public static partial void CreatingIntrospectionResponse(ILogger logger);

    [LoggerMessage(0x6649, LogLevel.Debug, "Creating introspection response for inactive token.")]
    public static partial void CreatingInactiveIntrospectionResponse(ILogger logger);

    [LoggerMessage(0x664A, LogLevel.Debug, "Creating introspection response for active token.")]
    public static partial void CreatingActiveIntrospectionResponse(ILogger logger);

    [LoggerMessage(0x664B, LogLevel.Error, "Expected scope {scopes} is missing in token")]
    public static partial void ExpectedScopeMissing(ILogger logger, IEnumerable<string> scopes);

    // AuthenticationRequestHandlerWrapper

    [LoggerMessage(0x664C, LogLevel.Debug, "Processing federated signout")]
    public static partial void ProcessingFederatedSignOut(ILogger logger);

    [LoggerMessage(0x664D, LogLevel.Debug, "Rendering signout callback iframe")]
    public static partial void RenderingSignOutCallbackIframe(ILogger logger);

    [LoggerMessage(0x664E, LogLevel.Debug, "No signout callback iframe to render")]
    public static partial void NoSignOutCallbackIframe(ILogger logger);

    // LocalApiAuthenticationHandler

    [LoggerMessage(0x664F, LogLevel.Trace, "HandleAuthenticateAsync called")]
    public static partial void HandleAuthenticateCalled(ILogger logger);

    [LoggerMessage(0x6650, LogLevel.Trace, "Token found: {token}")]
    public static partial void TokenFound(ILogger logger, string token);

    [LoggerMessage(0x6651, LogLevel.Trace, "Failed to validate the token")]
    public static partial void TokenValidationFailed(ILogger logger);

    [LoggerMessage(0x6652, LogLevel.Information, "Local API request rejected: {reason}")]
    public static partial void LocalApiRequestRejected(ILogger logger, string reason);

    [LoggerMessage(0x6653, LogLevel.Trace, "Successfully validated the token.")]
    public static partial void TokenValidated(ILogger logger);
}
