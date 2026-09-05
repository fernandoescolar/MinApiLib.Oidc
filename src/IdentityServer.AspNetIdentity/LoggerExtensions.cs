namespace IdentityServer.AspNetIdentity;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "No user found matching subject Id: {subjectId}", EventName = "NoUserFound")]
    public static partial void NoUserFound(this ILogger logger, string subjectId);

    [LoggerMessage(2, LogLevel.Information, "Credentials validated for username: {username}", EventName = "CredentialsValidated")]
    public static partial void CredentialsValidated(this ILogger logger, string username);

    [LoggerMessage(3, LogLevel.Information, "Authentication failed for username: {username}, reason: {reason}", EventName = "AuthenticationFailed")]
    public static partial void AuthenticationFailed(this ILogger logger, string username, string reason);

    [LoggerMessage(4, LogLevel.Information, "No user found matching username: {username}", EventName = "NoUserFoundForUserName")]
    public static partial void NoUserFoundForUserName(this ILogger logger, string username);
}
