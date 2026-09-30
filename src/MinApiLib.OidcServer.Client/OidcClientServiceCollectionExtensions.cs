using MinApiLib.OidcServer.Client;
using MinApiLib.OidcServer.Client.ClientCredentials;
using MinApiLib.OidcServer.Client.OpenIdConnect;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers access tokens for <see cref="HttpClient"/>: client credentials tokens and the signed-in user's tokens,
/// as bearer tokens or DPoP-bound.
/// </summary>
public static class OidcClientServiceCollectionExtensions
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

        services.AddOidcClientCore();
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

        builder.Services.AddOidcClientCore();
        builder.Services.TryAddSingleton<IClientCredentialsTokenManager, ClientCredentialsTokenManager>();
        return builder.AddHttpMessageHandler(services => new ClientCredentialsTokenHandler(
            clientName,
            services.GetRequiredService<IClientCredentialsTokenManager>(),
            services.GetRequiredService<DPoPNonceStore>(),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<ILogger<ClientCredentialsTokenHandler>>()));
    }

    /// <summary>
    /// Keeps the tokens of an OpenID Connect scheme in the session and refreshes them, so
    /// <see cref="AddUserAccessTokenHandler"/> can send them. With <see cref="OpenIdConnectTokenOptions.UseDPoP"/>,
    /// the user's tokens are DPoP-bound to a key of the session.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="scheme">The name of the OpenID Connect scheme, as in <c>AddOpenIdConnect(scheme, ...)</c>.</param>
    /// <param name="configure">Sets whether to use DPoP and when to refresh.</param>
    /// <remarks>
    /// It extends <c>options.Events</c> of the scheme, and turns on <c>SaveTokens</c>. Schemes that use
    /// <c>EventsType</c> are not supported.
    /// </remarks>
    public static IServiceCollection AddOpenIdConnectTokens(this IServiceCollection services, string scheme, Action<OpenIdConnectTokenOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(scheme);

        services.AddUserAccessTokenCore();
        var tokenOptions = services.AddOptions<OpenIdConnectTokenOptions>(scheme);
        if (configure != null)
        {
            tokenOptions.Configure(configure);
        }

        services.AddOptions<OpenIdConnectOptions>(scheme)
            .PostConfigure<IOptionsMonitor<OpenIdConnectTokenOptions>>((options, settings) => OpenIdConnectTokenEvents.Attach(options, scheme, settings.Get(scheme)));
        return services;
    }

    /// <summary>
    /// Sends the signed-in user's access token with each request of this HTTP client, refreshed when it is about to
    /// expire. Use it from the requests of a signed-in user, in a scheme registered with
    /// <see cref="AddOpenIdConnectTokens"/>.
    /// </summary>
    /// <param name="builder">The HTTP client.</param>
    /// <param name="signInScheme">The scheme that keeps the session, usually the cookie scheme. Defaults to the default authenticate scheme.</param>
    public static IHttpClientBuilder AddUserAccessTokenHandler(this IHttpClientBuilder builder, string? signInScheme = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddUserAccessTokenCore();
        return builder.AddHttpMessageHandler(services => new UserAccessTokenHandler(
            signInScheme,
            services.GetRequiredService<IHttpContextAccessor>(),
            services.GetRequiredService<IUserAccessTokenManager>(),
            services.GetRequiredService<DPoPNonceStore>(),
            services.GetRequiredService<TimeProvider>(),
            services.GetRequiredService<ILogger<UserAccessTokenHandler>>()));
    }

    private static void AddUserAccessTokenCore(this IServiceCollection services)
    {
        services.AddOidcClientCore();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IUserAccessTokenManager, UserAccessTokenManager>();
    }

    private static void AddOidcClientCore(this IServiceCollection services)
    {
        services.AddHttpClient(OidcClientDefaults.HttpClientName);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<DPoPNonceStore>();
        services.TryAddSingleton<DiscoveryCache>();
        services.TryAddSingleton<TokenEndpointClient>();
    }
}
