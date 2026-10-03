namespace MinApiLib.Oidc.Server.Features.Revocation;

internal static partial class LoggerExtensions
{
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
}
