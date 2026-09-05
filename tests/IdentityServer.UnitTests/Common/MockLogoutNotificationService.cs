namespace IdentityServer.UnitTests.Common;

public class MockLogoutNotificationService : ILogoutNotificationService
{
    public bool GetFrontChannelLogoutNotificationsUrlsCalled { get; set; }
    public List<string> FrontChannelLogoutNotificationsUrls { get; set; } = new List<string>();

    public bool SendBackChannelLogoutNotificationsCalled { get; set; }
    public List<IdentityServer.Services.BackChannelLogoutRequest> BackChannelLogoutRequests { get; set; } = new List<IdentityServer.Services.BackChannelLogoutRequest>();

    public Task<IEnumerable<string>> GetFrontChannelLogoutNotificationsUrlsAsync(LogoutNotificationContext context)
    {
        GetFrontChannelLogoutNotificationsUrlsCalled = true;
        return Task.FromResult(FrontChannelLogoutNotificationsUrls.AsEnumerable());
    }

    public Task<IEnumerable<IdentityServer.Services.BackChannelLogoutRequest>> GetBackChannelLogoutNotificationsAsync(LogoutNotificationContext context)
    {
        SendBackChannelLogoutNotificationsCalled = true;
        return Task.FromResult(BackChannelLogoutRequests.AsEnumerable());
    }
}
