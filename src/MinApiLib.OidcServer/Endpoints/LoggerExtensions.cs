namespace MinApiLib.OidcServer.Endpoints;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0x0F03, LogLevel.Debug, "Start authorize callback request")]
    public static partial void StartAuthorizeCallbackRequest(this ILogger logger);

    [LoggerMessage(0x0F04, LogLevel.Trace, "End Authorize Request. Result type: {resultType}")]
    public static partial void EndAuthorizeRequest(this ILogger logger, string resultType);

    [LoggerMessage(0x0F05, LogLevel.Debug, "Start authorize request")]
    public static partial void StartAuthorizeRequest(this ILogger logger);

    [LoggerMessage(0x0F06, LogLevel.Debug, "User in authorize request: {subjectId}")]
    public static partial void UserInAuthorizeRequest(this ILogger logger, string subjectId);

    [LoggerMessage(0x0F07, LogLevel.Debug, "No user present in authorize request")]
    public static partial void NoUserInAuthorizeRequest(this ILogger logger);

    [LoggerMessage(0x0F08, LogLevel.Debug, "ValidatedAuthorizeRequest\n{validationDetails}")]
    public static partial void ValidatedAuthorizeRequest(this ILogger logger, object validationDetails);

    [LoggerMessage(0x0F09, LogLevel.Debug, "Authorize endpoint response\n{details}")]
    public static partial void AuthorizeEndpointResponse(this ILogger logger, object details);

    [LoggerMessage(0x0F20, LogLevel.Trace, "Identity token issued for {clientId} / {subjectId}: {token}")]
    public static partial void IdentityTokenIssued(this ILogger logger, string clientId, string subjectId, string token);

    [LoggerMessage(0x0F21, LogLevel.Trace, "Code issued for {clientId} / {subjectId}: {token}")]
    public static partial void CodeIssued(this ILogger logger, string clientId, string subjectId, string token);

    [LoggerMessage(0x0F22, LogLevel.Trace, "Access token issued for {clientId} / {subjectId}: {token}")]
    public static partial void AccessTokenIssued(this ILogger logger, string clientId, string subjectId, string token);


    [LoggerMessage(0x1F01, LogLevel.Debug, "Rendering check session result")]
    public static partial void RenderingCheckSessionResult(this ILogger logger);

    [LoggerMessage(0x2F00, LogLevel.Trace, "Processing device authorize request.")]
    public static partial void ProcessingDeviceAuthorizeRequest(this ILogger logger);

    [LoggerMessage(0x2F01, LogLevel.Warning, "Invalid content type for device authorize endpoint")]
    public static partial void InvalidHttpRequestForDeviceAuthorizeEndpoint(this ILogger logger);

    [LoggerMessage(0x2F02, LogLevel.Debug, "Start device authorize request.")]
    public static partial void StartDeviceAuthorizeRequest(this ILogger logger);

    [LoggerMessage(0x2F03, LogLevel.Trace, "Calling into device authorize response generator: {type}")]
    public static partial void CallingIntoDeviceAuthorizeResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F04, LogLevel.Debug, "Device authorize request success.")]
    public static partial void DeviceAuthorizeRequestSuccess(this ILogger logger);

    [LoggerMessage(0x2F05, LogLevel.Error, "Device authorization error: {error}:{errorDescriptions}")]
    public static partial void DeviceAuthorizationError(this ILogger logger, string error, string errorDescriptions);

    [LoggerMessage(0x2F10, LogLevel.Trace, "Processing discovery request.")]
    public static partial void ProcessingDiscoveryRequest(this ILogger logger);

    [LoggerMessage(0x2F12, LogLevel.Debug, "Start discovery request")]
    public static partial void StartDiscoveryRequest(this ILogger logger);

    [LoggerMessage(0x2F13, LogLevel.Information, "Discovery endpoint disabled. 404.")]
    public static partial void DiscoveryEndpointDisabled(this ILogger logger);

    [LoggerMessage(0x2F14, LogLevel.Trace, "Calling into discovery response generator: {type}")]
    public static partial void CallingIntoDiscoveryResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F15, LogLevel.Trace, "Processing key discovery request.")]
    public static partial void ProcessingKeyDiscoveryRequest(this ILogger logger);

    [LoggerMessage(0x2F16, LogLevel.Debug, "Start key discovery request")]
    public static partial void StartKeyDiscoveryRequest(this ILogger logger);

    [LoggerMessage(0x2F17, LogLevel.Information, "Key discovery disabled. 404.")]
    public static partial void KeyDiscoveryDisabled(this ILogger logger);

    [LoggerMessage(0x2F19, LogLevel.Debug, "Processing signout callback request")]
    public static partial void ProcessingSignoutCallbackRequest(this ILogger logger);

    [LoggerMessage(0x2F20, LogLevel.Information, "Successful signout callback.")]
    public static partial void SuccessfulSignoutCallback(this ILogger logger);

    [LoggerMessage(0x2F21, LogLevel.Error, "Error validating signout callback: {error}")]
    public static partial void ErrorValidatingSignoutCallback(this ILogger logger, string error);

    [LoggerMessage(0x2F23, LogLevel.Debug, "Processing signout request for {subjectId}")]
    public static partial void ProcessingSignoutRequest(this ILogger logger, string subjectId);

    [LoggerMessage(0x2F24, LogLevel.Error, "Error processing end session request {error}")]
    public static partial void ErrorProcessingEndSessionRequest(this ILogger logger, string error);

    [LoggerMessage(0x2F25, LogLevel.Debug, "Success validating end session request from {clientId}")]
    public static partial void SuccessValidatingEndSessionRequest(this ILogger logger, string? clientId);

    [LoggerMessage(0x2F30, LogLevel.Trace, "Processing introspection request.", EventName = "ProcessingIntrospectionRequest")]
    public static partial void ProcessingIntrospectionRequest(this ILogger logger);

    [LoggerMessage(0x2F32, LogLevel.Warning, "Invalid media type for introspection endpoint")]
    public static partial void InvalidMediaTypeForIntrospectionEndpoint(this ILogger logger);

    [LoggerMessage(0x2F33, LogLevel.Debug, "Starting introspection request.")]
    public static partial void StartingIntrospectionRequest(this ILogger logger);

    [LoggerMessage(0x2F34, LogLevel.Error, "API unauthorized to call introspection endpoint. aborting.")]
    public static partial void APIUnauthorizedToCallIntrospectionEndpoint(this ILogger logger);

    [LoggerMessage(0x2F35, LogLevel.Error, "Malformed request body. aborting.")]
    public static partial void MalformedRequestBody(this ILogger logger);

    [LoggerMessage(0x2F36, LogLevel.Trace, "Calling into introspection request validator: {type}")]
    public static partial void CallingIntoIntrospectionRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F37, LogLevel.Trace, "Calling into introspection response generator: {type}")]
    public static partial void CallingIntoIntrospectionResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F38, LogLevel.Information, "Success token introspection. Token active: {tokenActive}, for API name: {apiName}")]
    public static partial void SuccessTokenIntrospection(this ILogger logger, bool tokenActive, string apiName);

    [LoggerMessage(0x2F39, LogLevel.Error, "Failed token introspection: {error}, for API name: {apiName}")]
    public static partial void FailedTokenIntrospection(this ILogger logger, string error, string apiName);

    [LoggerMessage(0x2F40, LogLevel.Trace, "Processing token request.")]
    public static partial void ProcessingTokenRequest(this ILogger logger);

    [LoggerMessage(0x2F41, LogLevel.Warning, "Invalid content type for token endpoint")]
    public static partial void InvalidHttpRequestForTokenEndpoint(this ILogger logger);

    [LoggerMessage(0x2F42, LogLevel.Debug, "Start token request.")]
    public static partial void StartTokenRequest(this ILogger logger);

    [LoggerMessage(0x2F43, LogLevel.Trace, "Calling into token request validator: {type}")]
    public static partial void CallingIntoTokenRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F44, LogLevel.Trace, "Calling into token request response generator: {type}")]
    public static partial void CallingIntoTokenRequestResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F45, LogLevel.Debug, "Token request success.")]
    public static partial void TokenRequestSuccess(this ILogger logger);

    [LoggerMessage(0x2F46, LogLevel.Trace, "Refresh token issued for {clientId} / {subjectId}: {token}")]
    public static partial void RefreshTokenIssued(this ILogger logger, string clientId, string subjectId, string token);

    [LoggerMessage(0x2F47, LogLevel.Trace, "Processing revocation request.")]
    public static partial void ProcessingRevocationRequest(this ILogger logger);

    [LoggerMessage(0x2F49, LogLevel.Warning, "Invalid media type")]
    public static partial void InvalidMediaType(this ILogger logger);

    [LoggerMessage(0x2F50, LogLevel.Debug, "Start revocation request.")]
    public static partial void StartRevocationRequest(this ILogger logger);

    [LoggerMessage(0x2F51, LogLevel.Trace, "Client validation successful")]
    public static partial void ClientValidationSuccessful(this ILogger logger);

    [LoggerMessage(0x2F52, LogLevel.Trace, "Calling into token revocation request validator: {type}")]
    public static partial void CallingIntoTokenRevocationRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F53, LogLevel.Trace, "Calling into token revocation response generator: {type}")]
    public static partial void CallingIntoTokenRevocationResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F54, LogLevel.Information, "Token revocation complete")]
    public static partial void TokenRevocationComplete(this ILogger logger);

    [LoggerMessage(0x2F55, LogLevel.Information, "No matching token found")]
    public static partial void NoMatchingTokenFound(this ILogger logger);

    [LoggerMessage(0x2F57, LogLevel.Debug, "Start userinfo request")]
    public static partial void StartUserInfoRequest(this ILogger logger);

    [LoggerMessage(0x2F58, LogLevel.Trace, "Calling into userinfo request validator: {type}")]
    public static partial void CallingIntoUserInfoRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F59, LogLevel.Trace, "Calling into userinfo response generator: {type}")]
    public static partial void CallingIntoUserInfoResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F60, LogLevel.Debug, "End userinfo request")]
    public static partial void EndUserInfoRequest(this ILogger logger);

    [LoggerMessage(0x2F61, LogLevel.Error, "No access token found.")]
    public static partial void NoAccessTokenFound(this ILogger logger);

    [LoggerMessage(0x2F62, LogLevel.Trace, "Processing nonce request.")]
    public static partial void ProcessingNonceRequest(this ILogger logger);

    [LoggerMessage(0x2F63, LogLevel.Warning, "Invalid content type for nonce endpoint")]
    public static partial void InvalidHttpRequestForNonceEndpoint(this ILogger logger);

    [LoggerMessage(0x2F64, LogLevel.Debug, "Start nonce request.")]
    public static partial void StartNonceRequest(this ILogger logger);

    [LoggerMessage(0x2F65, LogLevel.Trace, "Calling into nonce request validator: {type}")]
    public static partial void CallingIntoNonceRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F66, LogLevel.Trace, "Calling into nonce request response generator: {type}")]
    public static partial void CallingIntoNonceRequestResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F67, LogLevel.Debug, "Nonce request success.")]
    public static partial void NonceRequestSuccess(this ILogger logger);

    [LoggerMessage(0xFFFE, LogLevel.Error, "{message}")]
    public static partial void Error(this ILogger logger, string message);

    [LoggerMessage(0xFFFF, LogLevel.Information, "{message}")]
    public static partial void Information(this ILogger logger, object message);
}
