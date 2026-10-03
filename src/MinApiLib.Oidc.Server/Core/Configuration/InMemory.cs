namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering in-memory services
/// </summary>
public static class OidcServerBuilderExtensionsInMemory
{
    /// <summary>
    /// Adds the in memory caching.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryCaching(this IOidcServerBuilder builder)
    {
        builder.Services.TryAddSingleton<IMemoryCache, MemoryCache>();
        builder.Services.TryAddTransient(typeof(ICache<>), typeof(DefaultCache<>));

        return builder;
    }

    /// <summary>
    /// Adds the in memory identity resources.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="identityResources">The identity resources.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryIdentityResources(this IOidcServerBuilder builder, IEnumerable<IdentityResource> identityResources)
    {
        builder.Services.AddSingleton(identityResources);
        builder.AddResourceStore<InMemoryResourcesStore>();

        return builder;
    }

    /// <summary>
    /// Adds the in memory identity resources.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The configuration section containing the configuration data.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryIdentityResources(this IOidcServerBuilder builder, IConfigurationSection section)
    {
        var resources = new List<IdentityResource>();
        section.Bind(resources);

        return builder.AddInMemoryIdentityResources(resources);
    }

    /// <summary>
    /// Adds the in memory API resources.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="apiResources">The API resources.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryApiResources(this IOidcServerBuilder builder, IEnumerable<ApiResource> apiResources)
    {
        builder.Services.AddSingleton(apiResources);
        builder.AddResourceStore<InMemoryResourcesStore>();

        return builder;
    }

    /// <summary>
    /// Adds the in memory API resources.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The configuration section containing the configuration data.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryApiResources(this IOidcServerBuilder builder, IConfigurationSection section)
    {
        var resources = new List<ApiResource>();
        section.Bind(resources);

        return builder.AddInMemoryApiResources(resources);
    }

    /// <summary>
    /// Adds the in memory API scopes.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="apiScopes">The API scopes.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryApiScopes(this IOidcServerBuilder builder, IEnumerable<ApiScope> apiScopes)
    {
        builder.Services.AddSingleton(apiScopes);
        builder.AddResourceStore<InMemoryResourcesStore>();

        return builder;
    }

    /// <summary>
    /// Adds the in memory scopes.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The configuration section containing the configuration data.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryApiScopes(this IOidcServerBuilder builder, IConfigurationSection section)
    {
        var resources = new List<ApiScope>();
        section.Bind(resources);

        return builder.AddInMemoryApiScopes(resources);
    }

    /// <summary>
    /// Adds the in memory clients.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="clients">The clients.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryClients(this IOidcServerBuilder builder, IEnumerable<Client> clients)
    {
        builder.Services.AddSingleton(clients);

        builder.AddClientStore<InMemoryClientStore>();

        var existingCors = builder.Services.Where(x => x.ServiceType == typeof(ICorsPolicyService)).LastOrDefault();
        if (existingCors != null &&
            existingCors.ImplementationType == typeof(DefaultCorsPolicyService) &&
            existingCors.Lifetime == ServiceLifetime.Transient)
        {
            builder.Services.AddTransient<ICorsPolicyService, InMemoryCorsPolicyService>();
        }

        return builder;
    }

    /// <summary>
    /// Adds the in memory clients.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The configuration section containing the configuration data.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryClients(this IOidcServerBuilder builder, IConfigurationSection section)
    {
        var clients = new List<Client>();
        section.Bind(clients);

        return builder.AddInMemoryClients(clients);
    }

    /// <summary>
    /// Adds the in memory stores.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddInMemoryPersistedGrants(this IOidcServerBuilder builder)
    {
        builder.Services.TryAddSingleton<IPersistedGrantStore, InMemoryPersistedGrantStore>();
        builder.Services.TryAddSingleton<IDeviceFlowStore, InMemoryDeviceFlowStore>();

        return builder;
    }
}
