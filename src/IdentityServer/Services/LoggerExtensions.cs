namespace IdentityServer.Services;

internal static partial class LoggerExtensions
{
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
