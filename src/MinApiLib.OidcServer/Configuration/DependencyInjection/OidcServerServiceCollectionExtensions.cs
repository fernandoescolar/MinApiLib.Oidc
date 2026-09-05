namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI extension methods for adding OidcServer
/// </summary>
public static class OidcServerServiceCollectionExtensions
{
    /// <summary>
    /// Creates a builder.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddOidcServerBuilder(this IServiceCollection services)
    {
        return new OidcServerBuilder(services);
    }

    /// <summary>
    /// Adds OidcServer.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddOidcServer(this IServiceCollection services)
    {
        var builder = services.AddOidcServerBuilder();

        builder
            .AddRequiredPlatformServices()
            .AddCookieAuthentication()
            .AddDefaultServices();

        builder.AddInMemoryPersistedGrants();

        return builder;
    }

    /// <summary>
    /// Adds OidcServer.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddOidcServer(this IServiceCollection services, Action<OidcServerOptions> setupAction)
    {
        services.Configure(setupAction);
        return services.AddOidcServer();
    }

    /// <summary>
    /// Adds the OidcServer.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddOidcServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OidcServerOptions>(configuration);
        return services.AddOidcServer();
    }

    /// <summary>
    /// Configures the OpenIdConnect handlers to persist the state parameter into the server-side IDistributedCache.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="schemes">The schemes to configure. If none provided, then all OpenIdConnect schemes will use the cache.</param>
    public static IServiceCollection AddOidcStateDataFormatterCache(this IServiceCollection services, params string[] schemes)
    {
        services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>>(
            svcs => new ConfigureOpenIdConnectOptions(
                schemes,
                svcs.GetRequiredService<IHttpContextAccessor>())
        );

        return services;
    }
}
