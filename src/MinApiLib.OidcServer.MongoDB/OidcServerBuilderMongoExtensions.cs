using MinApiLib.OidcServer.MongoDB;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the MongoDB stores of MinApiLib.OidcServer.
/// </summary>
public static class OidcServerBuilderMongoExtensions
{
    /// <summary>
    /// Stores the configuration, the operational data and the signing keys in MongoDB.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">Configures the connection, the database and which stores to use.</param>
    /// <returns>The builder.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddOidcServer()
    ///     .AddMongoStores(options =&gt;
    ///     {
    ///         options.ConnectionString = builder.Configuration.GetConnectionString("Mongo");
    ///         options.DatabaseName = "login";
    ///     });
    /// </code>
    /// </example>
    public static IOidcServerBuilder AddMongoStores(this IOidcServerBuilder builder, Action<MongoStoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new MongoStoreOptions();
        configure?.Invoke(options);

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            builder.Services.TryAddSingleton<IMongoClient>(new MongoClient(options.ConnectionString));
        }

        builder.Services.AddSingleton(options);
        builder.Services.TryAddSingleton(sp =>
        {
            var client = sp.GetService<IMongoClient>()
                ?? throw new InvalidOperationException("No MongoDB connection: set MongoStoreOptions.ConnectionString or register an IMongoClient.");
            return new OidcServerMongoContext(client.GetDatabase(options.DatabaseName), options.CollectionPrefix);
        });

        if (options.UseForConfiguration)
        {
            builder.AddClientStore<MongoClientStore>();
            builder.AddResourceStore<MongoResourceStore>();
            builder.AddCorsPolicyService<MongoCorsPolicyService>();
        }

        if (options.UseForOperationalData)
        {
            builder.AddPersistedGrantStore<MongoPersistedGrantStore>();
            builder.AddDeviceFlowStore<MongoDeviceFlowStore>();
        }

        if (options.UseForSigningKeys)
        {
            builder.Services.AddSingleton<ISigningKeyStore, MongoSigningKeyStore>();
        }

        if (options.CreateIndexes)
        {
            builder.Services.AddHostedService<MongoIndexInitializer>();
        }

        return builder;
    }
}
