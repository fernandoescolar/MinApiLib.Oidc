namespace MinApiLib.Oidc.Server.Features.Introspection;

internal static partial class LoggerExtensions
{
    [LoggerMessage(0x2F30, LogLevel.Trace, "Processing introspection request.", EventName = "ProcessingIntrospectionRequest")]
    public static partial void ProcessingIntrospectionRequest(this ILogger logger);

    [LoggerMessage(0x2F32, LogLevel.Warning, "Invalid media type for introspection endpoint")]
    public static partial void InvalidMediaTypeForIntrospectionEndpoint(this ILogger logger);

    [LoggerMessage(0x2F33, LogLevel.Debug, "Starting introspection request.")]
    public static partial void StartingIntrospectionRequest(this ILogger logger);

    [LoggerMessage(0x2F34, LogLevel.Error, "API unauthorized to call introspection endpoint. aborting.")]
    public static partial void APIUnauthorizedToCallIntrospectionEndpoint(this ILogger logger);

    [LoggerMessage(0x2F36, LogLevel.Trace, "Calling into introspection request validator: {type}")]
    public static partial void CallingIntoIntrospectionRequestValidator(this ILogger logger, string? type);

    [LoggerMessage(0x2F37, LogLevel.Trace, "Calling into introspection response generator: {type}")]
    public static partial void CallingIntoIntrospectionResponseGenerator(this ILogger logger, string? type);

    [LoggerMessage(0x2F38, LogLevel.Information, "Success token introspection. Token active: {tokenActive}, for API name: {apiName}")]
    public static partial void SuccessTokenIntrospection(this ILogger logger, bool tokenActive, string apiName);

    [LoggerMessage(0x2F39, LogLevel.Error, "Failed token introspection: {error}, for API name: {apiName}")]
    public static partial void FailedTokenIntrospection(this ILogger logger, string error, string apiName);
}
