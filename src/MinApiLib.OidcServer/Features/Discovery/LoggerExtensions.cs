namespace MinApiLib.OidcServer.Features.Discovery;

internal static partial class LoggerExtensions
{
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
}
