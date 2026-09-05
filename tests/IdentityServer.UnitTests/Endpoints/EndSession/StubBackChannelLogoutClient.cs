namespace IdentityServer.UnitTests.Endpoints.EndSession;

internal class StubBackChannelLogoutClient : IBackChannelLogoutService
{
    public bool SendLogoutsWasCalled { get; set; }

    public Task SendLogoutNotificationsAsync(LogoutNotificationContext context)
    {
        SendLogoutsWasCalled = true;
        return Task.CompletedTask;
    }
}
