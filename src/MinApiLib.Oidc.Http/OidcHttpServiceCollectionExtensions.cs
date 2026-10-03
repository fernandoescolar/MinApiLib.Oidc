using MinApiLib.Oidc.Http;
using MinApiLib.Oidc.Http.ClientCredentials;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers client credentials access tokens for <see cref="HttpClient"/>, as bearer tokens or DPoP-bound.
/// </summary>
public static class OidcHttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers a client that gets its own access tokens with the client credentials grant. Send them with
    /// <see cref="AddClientCredentialsTokenHandler"/>, or get them from <see cref="IClientCredentialsTokenManager"/>.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="clientName">A name for the client, to refer to it from the HTTP clients.</param>
    /// <param name="configure">Sets the server, the client id and secret, the scopes and whether to use DPoP.</param>
    public static IServiceCollection AddClientCredentialsTokens(this IServiceCollection services, string clientName, Action<ClientCredentialsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(clientName);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddTokenEndpointClient();
        services.TryAddSingleton<IClientCredentialsTokenManager, ClientCredentialsTokenManager>();
        services.AddOptions<ClientCredentialsOptions>(clientName).Configure(configure);
        return services;
    }

    /// <summary>
    /// Sends the access token of a client credentials client with each request of this HTTP client.
    /// </summary>
    /// <param name="builder">The HTTP client.</param>
    /// <param name="clientName">The name the client was registered with in <see cref="AddClientCredentialsTokens"/>.</param>
    public static IHttpClientBuilder AddClientCredentialsTokenHandler(this IHttpClientBuilder builder, string clientName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(clientName);

        builder.Services.AddTokenEndpointClient();
        builder.Services.TryAddSingleton<IClientCredentialsTokenManager, ClientCredentialsTokenManager>();
        return builder.AddHttpMessageHandler(services => new ClientCredentialsTokenHandler(
            clientName,
            services.GetRequiredService<IClientCredentialsTokenManager>(),
            services.GetRequiredService<DPoPNonceStore>(),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<ILogger<ClientCredentialsTokenHandler>>()));
    }

    /// <summary>
    /// Registers what the token handlers share: <see cref="TokenEndpointClient"/>, the <see cref="DPoPNonceStore"/>,
    /// the <see cref="OidcHttpDefaults.HttpClientName"/> HTTP client and a <see cref="TimeProvider"/>. The other
    /// methods call it; call it yourself to build on <see cref="AccessTokenHandler"/>.
    /// </summary>
    /// <param name="services">The services.</param>
    public static IServiceCollection AddTokenEndpointClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(OidcHttpDefaults.HttpClientName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<DPoPNonceStore>();
        services.TryAddSingleton<DiscoveryCache>();
        services.TryAddSingleton<TokenEndpointClient>();
        return services;
    }
}
