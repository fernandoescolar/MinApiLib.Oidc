namespace IdentityServer.UnitTests.Endpoints.NonceEndpointTests;

internal class FakeEventService : IEventService
{
    public bool CanRaiseEventType(EventTypes evtType)
    {
        return true;
    }

    public Task RaiseAsync(Event evt)
    {
        return Task.CompletedTask;
    }
}
