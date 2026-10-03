namespace MinApiLib.Oidc.Server.Core.Logging;

internal static partial class RequestValidationLog
{
    // Token request grant validators

    [LoggerMessage(0x6000, LogLevel.Debug, "Start validation of authorization code token request")]
    public static partial void StartAuthorizationCodeValidation(ILogger logger);

    [LoggerMessage(0x6001, LogLevel.Debug, "Client required a proof key for code exchange. Starting PKCE validation")]
    public static partial void StartPkceValidation(ILogger logger);

    [LoggerMessage(0x6002, LogLevel.Debug, "Validation of authorization code token request success")]
    public static partial void AuthorizationCodeValidationSuccess(ILogger logger);

    [LoggerMessage(0x6003, LogLevel.Debug, "Start validation of refresh token request")]
    public static partial void StartRefreshTokenValidation(ILogger logger);

    [LoggerMessage(0x6004, LogLevel.Debug, "Validation of refresh token request success")]
    public static partial void RefreshTokenValidationSuccess(ILogger logger);

    [LoggerMessage(0x6005, LogLevel.Trace, "Client provided no scopes - checking allowed scopes list")]
    public static partial void NoScopesProvided(ILogger logger);

    [LoggerMessage(0x6006, LogLevel.Trace, "Defaulting to: {scopes}")]
    public static partial void DefaultingToScopes(ILogger logger, string scopes);

    [LoggerMessage(0x6007, LogLevel.Debug, "Start validation of custom grant token request")]
    public static partial void StartExtensionGrantValidation(ILogger logger);

    [LoggerMessage(0x6008, LogLevel.Debug, "Validation of extension grant token request success")]
    public static partial void ExtensionGrantValidationSuccess(ILogger logger);

    [LoggerMessage(0x6009, LogLevel.Debug, "Start validation of device code request")]
    public static partial void StartDeviceCodeValidation(ILogger logger);

    [LoggerMessage(0x600A, LogLevel.Debug, "Start client credentials token request validation")]
    public static partial void StartClientCredentialsValidation(ILogger logger);

    [LoggerMessage(0x600B, LogLevel.Debug, "{clientId} credentials token request validation success")]
    public static partial void ClientCredentialsValidationSuccess(ILogger logger, string clientId);

    // TokenRequestValidationContext (dynamic level)

    [LoggerMessage(EventId = 0x600C, Message = "{Message}, {@details}")]
    public static partial void TokenRequestValidationMessage(ILogger logger, LogLevel level, string message, object details);

    [LoggerMessage(EventId = 0x600D, Message = "{Message}{@values}, details: {@details}")]
    public static partial void TokenRequestValidationMessageWithValues(ILogger logger, LogLevel level, string message, object values, object details);

    [LoggerMessage(0x600E, LogLevel.Error, "Error logging {exception}, request details: {@details}")]
    public static partial void TokenRequestLoggingFailed(ILogger logger, string exception, object details);

    [LoggerMessage(EventId = 0x600F, Message = "{@details}")]
    public static partial void TokenRequestValidationDetails(ILogger logger, LogLevel level, object details);

    // TokenRequestValidator

    [LoggerMessage(0x6010, LogLevel.Debug, "Start token request validation")]
    public static partial void StartTokenRequestValidation(ILogger logger);

    [LoggerMessage(0x6011, LogLevel.Trace, "Calling into custom request validator: {type}")]
    public static partial void CallingCustomRequestValidator(ILogger logger, string type);

    // TokenValidator

    [LoggerMessage(0x6012, LogLevel.Error, "Unknown or disabled client: {clientId}.")]
    public static partial void UnknownOrDisabledClient(ILogger logger, string clientId);

    [LoggerMessage(0x6013, LogLevel.Debug, "Client found: {clientId} / {clientName}")]
    public static partial void ClientFound(ILogger logger, string clientId, string? clientName);

    [LoggerMessage(0x6014, LogLevel.Debug, "Calling into custom token validator: {type}")]
    public static partial void CallingCustomTokenValidator(ILogger logger, string type);

    [LoggerMessage(0x6015, LogLevel.Trace, "Start access token validation")]
    public static partial void StartAccessTokenValidation(ILogger logger);

    [LoggerMessage(0x6016, LogLevel.Error, "JWT too long")]
    public static partial void JwtTooLong(ILogger logger);

    [LoggerMessage(0x6017, LogLevel.Error, "token handle too long")]
    public static partial void TokenHandleTooLong(ILogger logger);

    [LoggerMessage(0x6018, LogLevel.Error, "Client deleted or disabled: {clientId}")]
    public static partial void ClientDeletedOrDisabled(ILogger logger, string clientId);

    [LoggerMessage(0x6019, LogLevel.Error, "User marked as not active: {subject}")]
    public static partial void UserMarkedNotActive(ILogger logger, string subject);

    [LoggerMessage(0x601A, LogLevel.Information, "JWT token validation error: {exception}")]
    public static partial void JwtTokenExpired(ILogger logger, Exception ex, string exception);

    [LoggerMessage(0x601B, LogLevel.Error, "JWT token validation error: {exception}")]
    public static partial void JwtTokenValidationFailed(ILogger logger, Exception ex, string exception);

    [LoggerMessage(0x601C, LogLevel.Error, "Malformed JWT token: {exception}")]
    public static partial void MalformedJwt(ILogger logger, Exception ex, string exception);

    [LoggerMessage(0x601D, LogLevel.Error, "{message}\n{@logMessage}")]
    public static partial void TokenValidationError(ILogger logger, string message, object logMessage);

    [LoggerMessage(0x601E, LogLevel.Debug, "Token validation success\n{@logMessage}")]
    public static partial void TokenValidationSuccess(ILogger logger, object logMessage);

    // AuthorizeRequestValidator

    [LoggerMessage(0x601F, LogLevel.Debug, "Start authorize request protocol validation")]
    public static partial void StartAuthorizeRequestValidation(ILogger logger);

    [LoggerMessage(0x6020, LogLevel.Debug, "Calling into custom validator: {type}")]
    public static partial void CallingCustomAuthorizeValidator(ILogger logger, string type);

    [LoggerMessage(0x6021, LogLevel.Trace, "Authorize request protocol validation successful")]
    public static partial void AuthorizeRequestValidationSuccess(ILogger logger);

    [LoggerMessage(0x6022, LogLevel.Debug, "Checking for PKCE parameters")]
    public static partial void CheckingPkceParameters(ILogger logger);

    [LoggerMessage(0x6023, LogLevel.Debug, "No PKCE used.")]
    public static partial void NoPkceUsed(ILogger logger);

    [LoggerMessage(0x6024, LogLevel.Debug, "Missing code_challenge_method, defaulting to plain")]
    public static partial void DefaultingToPlainCodeChallengeMethod(ILogger logger);

    [LoggerMessage(0x6025, LogLevel.Error, "Requests for id_token response type must include identity scopes")]
    public static partial void IdTokenRequiresIdentityScopes(ILogger logger);

    [LoggerMessage(0x6026, LogLevel.Error, "Requests for id_token response type only must not include resource scopes")]
    public static partial void IdTokenOnlyMustNotIncludeResourceScopes(ILogger logger);

    [LoggerMessage(0x6027, LogLevel.Error, "Requests for token response type only must include resource scopes, but no identity scopes.")]
    public static partial void TokenOnlyRequiresResourceScopes(ILogger logger);

    [LoggerMessage(0x6028, LogLevel.Debug, "Unsupported display mode - ignored: {display}")]
    public static partial void UnsupportedDisplayMode(ILogger logger, string display);

    [LoggerMessage(0x6029, LogLevel.Warning, "idp requested ({idp}) is not in client restriction list.")]
    public static partial void IdpNotInClientRestrictions(ILogger logger, string idp);

    [LoggerMessage(0x602A, LogLevel.Error, "{message}\n{@requestDetails}")]
    public static partial void AuthorizeRequestError(ILogger logger, string message, object requestDetails);

    [LoggerMessage(0x602B, LogLevel.Error, "{message}: {detail}\n{@requestDetails}")]
    public static partial void AuthorizeRequestErrorWithDetail(ILogger logger, string message, string detail, object requestDetails);

    // TokenRevocationRequestValidator

    [LoggerMessage(0x602C, LogLevel.Trace, "ValidateRequestAsync called")]
    public static partial void RevocationValidateRequestCalled(ILogger logger);

    [LoggerMessage(0x602D, LogLevel.Error, "no parameters passed")]
    public static partial void RevocationNoParameters(ILogger logger);

    [LoggerMessage(0x602E, LogLevel.Error, "no client passed")]
    public static partial void RevocationNoClient(ILogger logger);

    [LoggerMessage(0x602F, LogLevel.Error, "No token found in request")]
    public static partial void RevocationNoToken(ILogger logger);

    [LoggerMessage(0x6030, LogLevel.Debug, "Token type hint found in request: {tokenTypeHint}")]
    public static partial void TokenTypeHintFound(ILogger logger, string tokenTypeHint);

    [LoggerMessage(0x6031, LogLevel.Error, "Invalid token type hint: {tokenTypeHint}")]
    public static partial void InvalidTokenTypeHint(ILogger logger, string tokenTypeHint);

    [LoggerMessage(0x6032, LogLevel.Debug, "ValidateRequestAsync result: {validateRequestResult}")]
    public static partial void RevocationValidationResult(ILogger logger, TokenRevocationRequestValidationResult validateRequestResult);

    // DeviceCodeValidator

    [LoggerMessage(0x6033, LogLevel.Error, "Invalid device code")]
    public static partial void InvalidDeviceCode(ILogger logger);

    [LoggerMessage(0x6034, LogLevel.Error, "Client {clientId} is trying to use a device code from client {deviceCodeClientId}")]
    public static partial void DeviceCodeClientMismatch(ILogger logger, string clientId, string deviceCodeClientId);

    [LoggerMessage(0x6035, LogLevel.Error, "Client {clientId} is polling too fast")]
    public static partial void DeviceCodePollingTooFast(ILogger logger, string clientId);

    [LoggerMessage(0x6036, LogLevel.Error, "Expired device code")]
    public static partial void DeviceCodeExpired(ILogger logger);

    [LoggerMessage(0x6037, LogLevel.Error, "No scopes authorized for device authorization. Access denied")]
    public static partial void DeviceCodeNoScopesAuthorized(ILogger logger);

    [LoggerMessage(0x6038, LogLevel.Error, "User has been disabled: {subjectId}")]
    public static partial void DeviceCodeUserDisabled(ILogger logger, string subjectId);

    // DeviceAuthorizationRequestValidator

    [LoggerMessage(0x6039, LogLevel.Debug, "Start device authorization request validation")]
    public static partial void StartDeviceAuthorizationValidation(ILogger logger);

    [LoggerMessage(0x603A, LogLevel.Debug, "{clientId} device authorization request validation success")]
    public static partial void DeviceAuthorizationValidationSuccess(ILogger logger, string clientId);

    [LoggerMessage(0x603B, LogLevel.Error, "{message}\n{requestDetails}")]
    public static partial void DeviceAuthorizationRequestError(ILogger logger, string message, object requestDetails);

    [LoggerMessage(0x603C, LogLevel.Error, "{message}: {detail}\n{requestDetails}")]
    public static partial void DeviceAuthorizationRequestErrorWithDetail(ILogger logger, string message, string detail, object requestDetails);

    // DefaultResourceValidator

    [LoggerMessage(0x603D, LogLevel.Error, "Invalid parsed scope {scope}, message: {error}")]
    public static partial void InvalidParsedScope(ILogger logger, string scope, string error);

    [LoggerMessage(0x603E, LogLevel.Error, "Resource indicator {resourceIndicator} does not match an API resource of the requested scopes.")]
    public static partial void ResourceIndicatorNotMatched(ILogger logger, string resourceIndicator);

    [LoggerMessage(0x603F, LogLevel.Error, "Scope {scope} not found in store.")]
    public static partial void ScopeNotFound(ILogger logger, string scope);

    [LoggerMessage(0x6040, LogLevel.Error, "Client {client} is not allowed access to scope {scope}.")]
    public static partial void ClientNotAllowedScope(ILogger logger, string client, string scope);

    [LoggerMessage(0x6041, LogLevel.Error, "Client {client} is not allowed access to scope offline_access (via AllowOfflineAccess setting).")]
    public static partial void ClientNotAllowedOfflineAccess(ILogger logger, string client);

    // JwtRequestValidator

    [LoggerMessage(0x6042, LogLevel.Error, "Could not parse client secrets")]
    public static partial void CouldNotParseClientSecrets(ILogger logger, Exception exception);

    [LoggerMessage(0x6043, LogLevel.Error, "There are no keys available to validate JWT.")]
    public static partial void NoKeysToValidateJwt(ILogger logger);

    [LoggerMessage(0x6044, LogLevel.Error, "JWT token validation error")]
    public static partial void JwtRequestValidationError(ILogger logger, Exception exception);

    [LoggerMessage(0x6045, LogLevel.Error, "JWT payload must not contain request or request_uri")]
    public static partial void JwtPayloadContainsRequest(ILogger logger);

    [LoggerMessage(0x6046, LogLevel.Debug, "JWT request object validation success.")]
    public static partial void JwtRequestValidationSuccess(ILogger logger);

    // EndSessionRequestValidator

    [LoggerMessage(0x6047, LogLevel.Debug, "Start end session request validation")]
    public static partial void StartEndSessionValidation(ILogger logger);

    [LoggerMessage(0x6048, LogLevel.Warning, "Invalid PostLogoutRedirectUri: {postLogoutRedirectUri}")]
    public static partial void InvalidPostLogoutRedirectUri(ILogger logger, string postLogoutRedirectUri);

    [LoggerMessage(0x6049, LogLevel.Information, "{Message}\n{@details}")]
    public static partial void EndSessionValidationFailureWithDetails(ILogger logger, string message, object details);

    [LoggerMessage(0x604A, LogLevel.Information, "{Message}")]
    public static partial void EndSessionValidationFailure(ILogger logger, string message);

    [LoggerMessage(0x604B, LogLevel.Information, "End session request validation success\n{@details}")]
    public static partial void EndSessionValidationSuccess(ILogger logger, object details);

    // IntrospectionRequestValidator

    [LoggerMessage(0x604C, LogLevel.Debug, "Introspection request validation started.")]
    public static partial void IntrospectionValidationStarted(ILogger logger);

    [LoggerMessage(0x604D, LogLevel.Error, "Token is missing")]
    public static partial void IntrospectionTokenMissing(ILogger logger);

    [LoggerMessage(0x604E, LogLevel.Debug, "Token is invalid.")]
    public static partial void IntrospectionTokenInvalid(ILogger logger);

    [LoggerMessage(0x604F, LogLevel.Debug, "Introspection request validation successful.")]
    public static partial void IntrospectionValidationSuccess(ILogger logger);

    // DefaultDPoPProofValidator

    [LoggerMessage(0x6050, LogLevel.Debug, "Malformed DPoP proof")]
    public static partial void MalformedDPoPProof(ILogger logger, Exception exception);

    [LoggerMessage(0x6051, LogLevel.Debug, "Invalid jwk in DPoP proof")]
    public static partial void InvalidDPoPJwk(ILogger logger, Exception exception);

    [LoggerMessage(0x6052, LogLevel.Debug, "DPoP proof signature validation failed")]
    public static partial void DPoPSignatureValidationFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x6053, LogLevel.Information, "DPoP proof validation failed: {description}")]
    public static partial void DPoPValidationFailed(ILogger logger, string description);

    // BearerTokenUsageValidator

    [LoggerMessage(0x6054, LogLevel.Debug, "Bearer token found in header")]
    public static partial void BearerTokenFoundInHeader(ILogger logger);

    [LoggerMessage(0x6055, LogLevel.Debug, "Bearer token found in body")]
    public static partial void BearerTokenFoundInBody(ILogger logger);

    [LoggerMessage(0x6056, LogLevel.Debug, "Bearer token not found")]
    public static partial void BearerTokenNotFound(ILogger logger);

    [LoggerMessage(0x6057, LogLevel.Trace, "Unexpected header format: {header}")]
    public static partial void UnexpectedAuthorizationHeaderFormat(ILogger logger, string header);

    // ExtensionGrantValidator

    [LoggerMessage(0x6058, LogLevel.Error, "No validator found for grant type")]
    public static partial void NoExtensionGrantValidator(ILogger logger);

    [LoggerMessage(0x6059, LogLevel.Trace, "Calling into custom grant validator: {type}")]
    public static partial void CallingCustomGrantValidator(ILogger logger, string type);

    [LoggerMessage(0x605A, LogLevel.Error, "Grant validation error: {message}")]
    public static partial void GrantValidationError(ILogger logger, Exception exception, string message);

    // UserInfoRequestValidator

    [LoggerMessage(0x605B, LogLevel.Error, "Token contains no sub claim")]
    public static partial void UserInfoNoSubClaim(ILogger logger);

    [LoggerMessage(0x605C, LogLevel.Error, "User is not active: {sub}")]
    public static partial void UserInfoUserNotActive(ILogger logger, string sub);

    // StrictRedirectUriValidatorAppAuth

    [LoggerMessage(0x605D, LogLevel.Debug, "{requestedUri} is not a valid loopback redirect URI")]
    public static partial void InvalidLoopbackRedirectUri(ILogger logger, string requestedUri);

    [LoggerMessage(0x605E, LogLevel.Debug, "{requestedUri} does not match a registered loopback redirect URI")]
    public static partial void UnregisteredLoopbackRedirectUri(ILogger logger, string requestedUri);

    // DefaultScopeParser

    [LoggerMessage(0x605F, LogLevel.Debug, "Scope parsing ignoring scope {scope}")]
    public static partial void ScopeParsingIgnored(ILogger logger, string scope);

    // Resource owner password grant (deprecated)

    [LoggerMessage(0x6060, LogLevel.Debug, "Start resource owner password token request validation")]
    public static partial void StartResourceOwnerPasswordValidation(ILogger logger);

    [LoggerMessage(0x6061, LogLevel.Debug, "Resource owner password token request validation success.")]
    public static partial void ResourceOwnerPasswordValidationSuccess(ILogger logger);

    [LoggerMessage(0x6062, LogLevel.Information, "Resource owner password credential type not supported. Configure an IResourceOwnerPasswordValidator.")]
    public static partial void ResourceOwnerPasswordNotSupported(ILogger logger);
}
