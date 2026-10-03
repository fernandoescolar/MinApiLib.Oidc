namespace MinApiLib.Oidc.Server.Features.UserInfo;

internal static partial class LoggerExtensions
{
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
}
