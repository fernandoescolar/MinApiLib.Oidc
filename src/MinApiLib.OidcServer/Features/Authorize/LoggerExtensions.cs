namespace MinApiLib.OidcServer.Features.Authorize;

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

    [LoggerMessage(0x0F21, LogLevel.Trace, "Code issued for {clientId} / {subjectId}: {token}")]
    public static partial void CodeIssued(this ILogger logger, string clientId, string subjectId, string token);

    [LoggerMessage(0xFFFE, LogLevel.Error, "{message}")]
    public static partial void Error(this ILogger logger, string message);

    [LoggerMessage(0xFFFF, LogLevel.Information, "{message}")]
    public static partial void Information(this ILogger logger, object message);

    [LoggerMessage(0x3F00, LogLevel.Error, "request_uri {url} is not a valid HTTPS URL or targets a private network")]
    public static partial void JwtRequestUriInvalidUrl(this ILogger logger, string url);

    [LoggerMessage(0x3F01, LogLevel.Error, "Invalid content type {mediaType} from jwt url {url}")]
    public static partial void JwtRequestUriInvalidContentType(this ILogger logger, string mediaType, string url);

    [LoggerMessage(0x3F02, LogLevel.Error, "JWT response from {url} Content-Length {size} exceeds the {maxSize} byte limit")]
    public static partial void JwtRequestUriContentLengthExceeded(this ILogger logger, string url, long size, int maxSize);

    [LoggerMessage(0x3F03, LogLevel.Debug, "Success http response from jwt url {url}")]
    public static partial void JwtRequestUriSuccess(this ILogger logger, string url);

    [LoggerMessage(0x3F04, LogLevel.Error, "JWT response from {url} body size {size} exceeds the {maxSize} byte limit")]
    public static partial void JwtRequestUriBodySizeExceeded(this ILogger logger, string url, int size, int maxSize);

    [LoggerMessage(0x3F05, LogLevel.Error, "Invalid http status code {status} from jwt url {url}")]
    public static partial void JwtRequestUriInvalidStatusCode(this ILogger logger, HttpStatusCode status, string url);

    [LoggerMessage(0x3F06, LogLevel.Error, "Fetching jwt from request_uri {url} failed")]
    public static partial void JwtRequestUriFetchFailed(this ILogger logger, Exception exception, string url);
}
