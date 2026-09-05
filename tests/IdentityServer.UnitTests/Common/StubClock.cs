namespace IdentityServer.UnitTests.Common;

internal class StubClock : TimeProvider
{
    public Func<DateTime> UtcNowFunc = () => DateTime.UtcNow;
    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(UtcNowFunc());
}
