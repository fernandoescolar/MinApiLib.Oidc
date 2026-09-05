namespace MinApiLib.OidcServer.Configuration.DependencyInjection;

/// <summary>
/// Decorates an existing registration with keyed services: the original registration is moved under
/// <see cref="InnerServiceKey"/>, keeping its lifetime, and the decorator receives it through
/// <c>[FromKeyedServices(InnerServiceKey)]</c>. No reflection is needed, so this is trimming and AOT friendly.
/// </summary>
internal static class DecoratorServiceCollectionExtensions
{
    /// <summary>
    /// Key of the decorated (inner) registration.
    /// </summary>
    public const string InnerServiceKey = "MinApiLib.OidcServer.DecoratedService";

    public static void AddTransientDecorator<TService, TDecorator>(this IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        var registration = services.LastOrDefault(x => x.ServiceType == typeof(TService) && !x.IsKeyedService);
        if (registration == null)
        {
            throw new InvalidOperationException("Service type: " + typeof(TService).Name + " not registered.");
        }

        if (services.Any(x => x.ServiceType == typeof(TService) && x.IsKeyedService && Equals(x.ServiceKey, InnerServiceKey)))
        {
            throw new InvalidOperationException("Decorator already registered for type: " + typeof(TService).Name + ".");
        }

        services.Remove(registration);
        services.Add(AsKeyed(registration));
        services.AddTransient<TService, TDecorator>();
    }

    private static ServiceDescriptor AsKeyed(ServiceDescriptor registration)
    {
        if (registration.ImplementationInstance != null)
        {
            return new ServiceDescriptor(registration.ServiceType, InnerServiceKey, registration.ImplementationInstance);
        }

        if (registration.ImplementationFactory != null)
        {
            var factory = registration.ImplementationFactory;
            return new ServiceDescriptor(registration.ServiceType, InnerServiceKey, (provider, _) => factory(provider), registration.Lifetime);
        }

        // a descriptor without instance or factory always has an implementation type
        return new ServiceDescriptor(registration.ServiceType, InnerServiceKey, registration.ImplementationType!, registration.Lifetime);
    }
}
