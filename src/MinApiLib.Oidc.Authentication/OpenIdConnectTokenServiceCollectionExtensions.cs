using MinApiLib.Oidc.Authentication;
using MinApiLib.Oidc.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the signed-in user's access tokens for <see cref="HttpClient"/>, as bearer tokens or DPoP-bound.
/// </summary>
public static class OpenIdConnectTokenServiceCollectionExtensions
{
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
        services.AddTokenEndpointClient();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IUserAccessTokenManager, UserAccessTokenManager>();
    }
}
