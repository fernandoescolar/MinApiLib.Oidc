namespace IdentityServer.UnitTests.Common;

/// <summary>
/// Minimal IServiceProvider for unit tests that avoids calling BuildServiceProvider().
/// </summary>
internal class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void Add<T>(T instance) => _services[typeof(T)] = instance;

    public object GetService(Type serviceType) =>
        _services.TryGetValue(serviceType, out var service) ? service : null;
}
