namespace MinApiLib.OidcServer.Services;

/// <summary>
/// Extension for IUserSession.
/// </summary>
public static class IUserSessionExtensions
{
    /// <summary>
    /// Creates a LogoutNotificationContext for the current user session.
    /// </summary>
    /// <returns></returns>
    public static async Task<LogoutNotificationContext?> GetLogoutNotificationContext(this IUserSession session)
    {
        var clientIds = await session.GetClientListAsync();

        if (clientIds.Any())
        {
            // the session only has a client list while a user is signed in
            var user = await session.GetUserAsync();
            var sub = user!.GetSubjectId();
            var sid = await session.GetSessionIdAsync();

            return new LogoutNotificationContext
            {
                SubjectId = sub,
                SessionId = sid,
                ClientIds = clientIds
            };
        }

        return null;
    }
}
