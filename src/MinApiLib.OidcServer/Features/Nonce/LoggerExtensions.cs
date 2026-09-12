namespace MinApiLib.OidcServer.Features.Nonce;

internal static partial class LoggerExtensions
{
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
}
