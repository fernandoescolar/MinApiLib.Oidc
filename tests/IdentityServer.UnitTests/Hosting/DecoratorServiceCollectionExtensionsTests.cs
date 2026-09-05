using IdentityServer.Configuration.DependencyInjection;

namespace IdentityServer.UnitTests.Hosting;

public class DecoratorServiceCollectionExtensionsTests
{
    public interface IGreeter
    {
        string Greet();
    }

    public sealed class Greeter : IGreeter
    {
        public string Greet() => "hello";
    }

    public sealed class ShoutingGreeter : IGreeter
    {
        private readonly IGreeter _inner;

        public ShoutingGreeter([FromKeyedServices(DecoratorServiceCollectionExtensions.InnerServiceKey)] IGreeter inner)
        {
            _inner = inner;
        }

        public string Greet() => _inner.Greet().ToUpperInvariant();
    }

    public static TheoryData<string> Registrations => new() { "type", "factory", "instance" };

    [Theory]
    [MemberData(nameof(Registrations))]
    public void Decorator_wraps_type_factory_and_instance_registrations(string registration)
    {
        var services = new ServiceCollection();
        switch (registration)
        {
            case "type": services.AddScoped<IGreeter, Greeter>(); break;
            case "factory": services.AddScoped<IGreeter>(_ => new Greeter()); break;
            default: services.AddSingleton<IGreeter>(new Greeter()); break;
        }

        services.AddTransientDecorator<IGreeter, ShoutingGreeter>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Equal("HELLO", scope.ServiceProvider.GetRequiredService<IGreeter>().Greet());
        Assert.IsType<Greeter>(scope.ServiceProvider.GetRequiredKeyedService<IGreeter>(DecoratorServiceCollectionExtensions.InnerServiceKey));
    }

    [Fact]
    public void Decorated_registration_keeps_its_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter, Greeter>();

        services.AddTransientDecorator<IGreeter, ShoutingGreeter>();

        Assert.Equal(ServiceLifetime.Scoped, services.Single(d => d.ServiceType == typeof(IGreeter) && d.IsKeyedService).Lifetime);
    }

    [Fact]
    public void Decorating_twice_should_throw()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter, Greeter>();
        services.AddTransientDecorator<IGreeter, ShoutingGreeter>();

        var act = () => services.AddTransientDecorator<IGreeter, ShoutingGreeter>();

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void Decorating_an_unregistered_service_should_throw()
    {
        var act = () => new ServiceCollection().AddTransientDecorator<IGreeter, ShoutingGreeter>();

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void Required_platform_services_register_the_system_clock()
    {
        var services = new ServiceCollection();

        new IdentityServerBuilder(services).AddRequiredPlatformServices();

        Assert.Contains(services, d => d.ServiceType == typeof(TimeProvider));
    }
}
