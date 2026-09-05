namespace IdentityServer.UnitTests.Common;

public class TestEventService : IEventService
{
    private Dictionary<Type, object> _events = new Dictionary<Type, object>();

    public Task RaiseAsync(Event evt)
    {
        _events.Add(evt.GetType(), evt);
        return Task.CompletedTask;
    }

    public T AssertEventWasRaised<T>()
        where T : class
    {
        Assert.True(_events.ContainsKey(typeof(T)));
        return (T)_events.Where(x => x.Key == typeof(T)).Select(x => x.Value).First();
    }

    public bool CanRaiseEventType(EventTypes evtType)
    {
        return true;
    }
}
