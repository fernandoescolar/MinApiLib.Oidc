namespace IdentityServer.UnitTests.Common;

public static class TestLogger
{
    public static ILogger<T> Create<T>()
    {
        return new LoggerFactory().CreateLogger<T>();
    }
}
