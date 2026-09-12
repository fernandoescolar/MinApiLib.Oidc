namespace MinApiLib.OidcServer.Features.DeviceFlow;

internal static partial class LoggerExtensions
{
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
}
