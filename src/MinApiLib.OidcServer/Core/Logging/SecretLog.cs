namespace MinApiLib.OidcServer.Core.Logging;

internal static partial class SecretLog
{
    [LoggerMessage(0x6200, LogLevel.Trace, "Start API validation")]
    public static partial void StartApiValidation(ILogger logger);

    [LoggerMessage(0x6201, LogLevel.Error, "No API secret found")]
    public static partial void NoApiSecretFound(ILogger logger);

    [LoggerMessage(0x6202, LogLevel.Error, "No API resource with that name found. aborting")]
    public static partial void ApiResourceNotFound(ILogger logger);

    [LoggerMessage(0x6203, LogLevel.Error, "More than one API resource with that name found. aborting")]
    public static partial void MultipleApiResourcesFound(ILogger logger);

    [LoggerMessage(0x6204, LogLevel.Error, "API resource not enabled. aborting.")]
    public static partial void ApiResourceNotEnabled(ILogger logger);

    [LoggerMessage(0x6205, LogLevel.Debug, "API resource validation success")]
    public static partial void ApiResourceValidationSuccess(ILogger logger);

    [LoggerMessage(0x6206, LogLevel.Error, "API validation failed.")]
    public static partial void ApiValidationFailed(ILogger logger);

    [LoggerMessage(0x6207, LogLevel.Debug, "Start parsing Basic Authentication secret")]
    public static partial void StartParsingBasicAuthentication(ILogger logger);

    [LoggerMessage(0x6208, LogLevel.Warning, "Malformed Basic Authentication credential.")]
    public static partial void MalformedBasicAuthenticationCredential(ILogger logger);

    [LoggerMessage(0x6209, LogLevel.Error, "Client ID exceeds maximum length.")]
    public static partial void ClientIdTooLong(ILogger logger);

    [LoggerMessage(0x620A, LogLevel.Error, "Client secret exceeds maximum length.")]
    public static partial void ClientSecretTooLong(ILogger logger);

    [LoggerMessage(0x620B, LogLevel.Debug, "client id without secret found")]
    public static partial void ClientIdWithoutSecretFound(ILogger logger);

    [LoggerMessage(0x620C, LogLevel.Debug, "No Basic Authentication secret found")]
    public static partial void NoBasicAuthenticationSecretFound(ILogger logger);

    [LoggerMessage(0x620D, LogLevel.Debug, "Start client validation")]
    public static partial void StartClientValidation(ILogger logger);

    [LoggerMessage(0x620E, LogLevel.Error, "No client identifier found")]
    public static partial void NoClientIdentifierFound(ILogger logger);

    [LoggerMessage(0x620F, LogLevel.Error, "No client with id '{clientId}' found. aborting")]
    public static partial void ClientNotFound(ILogger logger, string clientId);

    [LoggerMessage(0x6210, LogLevel.Debug, "Public Client - skipping secret validation success")]
    public static partial void PublicClientSkippingSecretValidation(ILogger logger);

    [LoggerMessage(0x6211, LogLevel.Error, "Client secret validation failed for client: {clientId}.")]
    public static partial void ClientSecretValidationFailed(ILogger logger, string clientId);

    [LoggerMessage(0x6212, LogLevel.Debug, "Client validation success")]
    public static partial void ClientValidationSuccess(ILogger logger);

    [LoggerMessage(0x6213, LogLevel.Debug, "Hashed shared secret validator cannot process {type}")]
    public static partial void HashedSharedSecretCannotProcess(ILogger logger, string type);

    [LoggerMessage(0x6214, LogLevel.Debug, "No shared secret configured for client.")]
    public static partial void NoSharedSecretConfigured(ILogger logger);

    [LoggerMessage(0x6215, LogLevel.Information, "Secret: {description} uses invalid hashing algorithm.")]
    public static partial void SecretInvalidHashingAlgorithm(ILogger logger, string description);

    [LoggerMessage(0x6216, LogLevel.Information, "Secret: {description} is null.")]
    public static partial void SecretIsNull(ILogger logger, string description);

    [LoggerMessage(0x6217, LogLevel.Debug, "No matching hashed secret found.")]
    public static partial void NoMatchingHashedSecret(ILogger logger);

    [LoggerMessage(0x6218, LogLevel.Debug, "Start parsing for JWT client assertion in post body")]
    public static partial void StartParsingJwtClientAssertion(ILogger logger);

    [LoggerMessage(0x6219, LogLevel.Debug, "Content type is not a form")]
    public static partial void ContentTypeNotForm(ILogger logger);

    [LoggerMessage(0x621A, LogLevel.Error, "Client assertion token exceeds maximum length.")]
    public static partial void ClientAssertionTooLong(ILogger logger);

    [LoggerMessage(0x621B, LogLevel.Debug, "No JWT client assertion found in post body")]
    public static partial void NoJwtClientAssertionFound(ILogger logger);

    [LoggerMessage(0x621C, LogLevel.Warning, "Could not parse client assertion")]
    public static partial void CouldNotParseClientAssertion(ILogger logger, Exception exception);

    [LoggerMessage(0x621D, LogLevel.Debug, "Start parsing for client id in post body")]
    public static partial void StartParsingClientIdInPostBody(ILogger logger);

    [LoggerMessage(0x621E, LogLevel.Debug, "Client certificate was not authenticated by an mTLS endpoint")]
    public static partial void ClientCertificateNotAuthenticatedByMtls(ILogger logger);

    [LoggerMessage(0x621F, LogLevel.Debug, "Client certificate not present")]
    public static partial void ClientCertificateNotPresent(ILogger logger);

    [LoggerMessage(0x6220, LogLevel.Debug, "No post body found")]
    public static partial void NoPostBodyFound(ILogger logger);

    [LoggerMessage(0x6221, LogLevel.Error, "Parsed secret should not be of type: {type}")]
    public static partial void InvalidParsedSecretType(ILogger logger, string type);

    [LoggerMessage(0x6222, LogLevel.Debug, "No matching plain text secret found.")]
    public static partial void NoMatchingPlainTextSecret(ILogger logger);

    [LoggerMessage(0x6223, LogLevel.Debug, "Start parsing for secret in post body")]
    public static partial void StartParsingSecretInPostBody(ILogger logger);

    [LoggerMessage(0x6224, LogLevel.Debug, "No secret in post body found")]
    public static partial void NoSecretInPostBody(ILogger logger);

    [LoggerMessage(0x6225, LogLevel.Error, "ParsedSecret.Credential is not a string.")]
    public static partial void CredentialNotString(ILogger logger);

    [LoggerMessage(0x6226, LogLevel.Error, "Could not parse secrets")]
    public static partial void CouldNotParseSecrets(ILogger logger, Exception exception);

    [LoggerMessage(0x6227, LogLevel.Error, "There are no keys available to validate client assertion.")]
    public static partial void NoKeysToValidateClientAssertion(ILogger logger);

    [LoggerMessage(0x6228, LogLevel.Error, "Both 'sub' and 'iss' in the client assertion token must have a value of client_id.")]
    public static partial void InvalidSubOrIss(ILogger logger);

    [LoggerMessage(0x6229, LogLevel.Error, "exp is missing.")]
    public static partial void ExpMissing(ILogger logger);

    [LoggerMessage(0x622A, LogLevel.Error, "jti is missing.")]
    public static partial void JtiMissing(ILogger logger);

    [LoggerMessage(0x622B, LogLevel.Error, "jti is found in replay cache. Possible replay attack.")]
    public static partial void JtiReplayDetected(ILogger logger);

    [LoggerMessage(0x622C, LogLevel.Error, "JWT token validation error")]
    public static partial void JwtTokenValidationError(ILogger logger, Exception exception);

    [LoggerMessage(0x622D, LogLevel.Debug, "Parser found secret: {type}")]
    public static partial void ParserFoundSecret(ILogger logger, string type);

    [LoggerMessage(0x622E, LogLevel.Error, "Client used more than one authentication method in the same request.")]
    public static partial void MultipleAuthenticationMethods(ILogger logger);

    [LoggerMessage(0x622F, LogLevel.Debug, "Secret id found: {id}")]
    public static partial void SecretIdFound(ILogger logger, string id);

    [LoggerMessage(0x6230, LogLevel.Debug, "Parser found no secret")]
    public static partial void ParserFoundNoSecret(ILogger logger);

    [LoggerMessage(0x6231, LogLevel.Information, "Secret [{description}] is expired")]
    public static partial void SecretExpired(ILogger logger, string description);

    [LoggerMessage(0x6232, LogLevel.Debug, "Secret validator success: {validatorType}")]
    public static partial void SecretValidatorSuccess(ILogger logger, string validatorType);

    [LoggerMessage(0x6233, LogLevel.Debug, "Secret validators could not validate secret")]
    public static partial void SecretValidatorsFailed(ILogger logger);

    [LoggerMessage(0x6234, LogLevel.Debug, "X509 name secret validator cannot process {type}")]
    public static partial void X509NameCannotProcess(ILogger logger, string type);

    [LoggerMessage(0x6235, LogLevel.Warning, "No subject/name found in X509 certificate.")]
    public static partial void NoX509SubjectName(ILogger logger);

    [LoggerMessage(0x6236, LogLevel.Debug, "No x509 name secrets configured for client.")]
    public static partial void NoX509NameSecretsConfigured(ILogger logger);

    [LoggerMessage(0x6237, LogLevel.Debug, "No matching x509 name secret found.")]
    public static partial void NoMatchingX509NameSecret(ILogger logger);

    [LoggerMessage(0x6238, LogLevel.Debug, "X509 thumbprint secret validator cannot process {type}")]
    public static partial void X509ThumbprintCannotProcess(ILogger logger, string type);

    [LoggerMessage(0x6239, LogLevel.Warning, "No thumbprint found in X509 certificate.")]
    public static partial void NoX509Thumbprint(ILogger logger);

    [LoggerMessage(0x623A, LogLevel.Debug, "No thumbprint secrets configured for client.")]
    public static partial void NoX509ThumbprintSecretsConfigured(ILogger logger);

    [LoggerMessage(0x623B, LogLevel.Debug, "No matching x509 thumbprint secret found.")]
    public static partial void NoMatchingX509ThumbprintSecret(ILogger logger);

    [LoggerMessage(0x623C, LogLevel.Trace, "Calling into client configuration validator: {validatorType}")]
    public static partial void CallingClientConfigurationValidator(ILogger logger, string validatorType);

    [LoggerMessage(0x623D, LogLevel.Debug, "client configuration validation for client {clientId} succeeded.")]
    public static partial void ClientConfigurationValidationSucceeded(ILogger logger, string clientId);

    [LoggerMessage(0x623E, LogLevel.Error, "Invalid client configuration for client {clientId}: {errorMessage}")]
    public static partial void InvalidClientConfiguration(ILogger logger, string clientId, string? errorMessage);

    [LoggerMessage(0x623F, LogLevel.Error, "Failed to deserialize JSON from grant store.")]
    public static partial void GrantDeserializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x6240, LogLevel.Debug, "{grantType} grant with value: {key} not found in store.")]
    public static partial void GrantNotFound(ILogger logger, string grantType, string key);

    [LoggerMessage(0x6241, LogLevel.Error, "Exception reading protected message")]
    public static partial void ProtectedMessageReadFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x6242, LogLevel.Error, "Exception writing protected message")]
    public static partial void ProtectedMessageWriteFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x6243, LogLevel.Error, "Could not read signing key file {file}")]
    public static partial void SigningKeyFileReadFailed(ILogger logger, Exception exception, string file);

    [LoggerMessage(0x6244, LogLevel.Trace, "Cache miss for {cacheKey}")]
    public static partial void CacheMiss(ILogger logger, string cacheKey);

    [LoggerMessage(0x6245, LogLevel.Trace, "Setting item in cache for {cacheKey}")]
    public static partial void CacheSet(ILogger logger, string cacheKey);

    [LoggerMessage(0x6246, LogLevel.Trace, "Cache hit for {cacheKey}")]
    public static partial void CacheHit(ILogger logger, string cacheKey);

    [LoggerMessage(0x6247, LogLevel.Debug, "Get profile called for subject {subject} from client {client} with claim types {claimTypes} via {caller}")]
    public static partial void ProfileRequested(ILogger logger, string subject, string client, IEnumerable<string> claimTypes, string caller);

    [LoggerMessage(0x6248, LogLevel.Debug, "Issued claims: {claims}")]
    public static partial void IssuedClaims(ILogger logger, IEnumerable<string> claims);

    [LoggerMessage(0x6249, LogLevel.Critical, "Error creating a JSON valued claim")]
    public static partial void JsonClaimCreationFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x624A, LogLevel.Trace, "Protecting message: {json}")]
    public static partial void ProtectingMessage(ILogger logger, string json);

    [LoggerMessage(0x624B, LogLevel.Error, "Error unprotecting message cookie")]
    public static partial void MessageCookieUnprotectFailed(ILogger logger, Exception exception);

    [LoggerMessage(0x624C, LogLevel.Debug, "Unable to unprotect cookie {cookieName}")]
    public static partial void CookieUnprotectFailed(ILogger logger, Exception exception, string cookieName);

    [LoggerMessage(0x624D, LogLevel.Trace, "Purging stale cookie: {cookieName}")]
    public static partial void PurgingStaleCookie(ILogger logger, string cookieName);

    [LoggerMessage(0x624E, LogLevel.Warning, "Endpoint disabled: {endpoint}")]
    public static partial void EndpointDisabled(ILogger logger, string endpoint);

    [LoggerMessage(0x624F, LogLevel.Information, "Userinfo request rejected: {reason}")]
    public static partial void UserInfoRequestRejected(ILogger logger, string reason);

    [LoggerMessage(0x6250, LogLevel.Debug, "Processing pushed authorization request")]
    public static partial void ProcessingPushedAuthorizationRequest(ILogger logger);

    [LoggerMessage(0x6251, LogLevel.Error, "client_id {clientId} in the pushed parameters does not match the authenticated client {authenticatedClientId}")]
    public static partial void PushedClientIdMismatch(ILogger logger, string clientId, string authenticatedClientId);

    [LoggerMessage(0x6252, LogLevel.Error, "Pushed authorization request validation failed: {error} {errorDescription}")]
    public static partial void PushedAuthorizationValidationFailed(ILogger logger, string error, string? errorDescription);

    [LoggerMessage(0x6253, LogLevel.Debug, "Pushed authorization request stored for client {clientId}")]
    public static partial void PushedAuthorizationRequestStored(ILogger logger, string clientId);
}
