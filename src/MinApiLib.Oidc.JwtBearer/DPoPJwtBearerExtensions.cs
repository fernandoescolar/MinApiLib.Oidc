using MinApiLib.Oidc.JwtBearer;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds DPoP (RFC 9449) to JWT bearer authentication.
/// </summary>
public static class DPoPJwtBearerExtensions
{
    /// <summary>Adds DPoP to the <see cref="JwtBearerDefaults.AuthenticationScheme"/> scheme.</summary>
    /// <param name="builder">The authentication builder, after <c>AddJwtBearer</c>.</param>
    /// <param name="configure">Sets which tokens the scheme accepts, the proof algorithms and nonces.</param>
    public static AuthenticationBuilder AddDPoP(this AuthenticationBuilder builder, Action<DPoPJwtBearerOptions>? configure = null)
        => builder.AddDPoP(JwtBearerDefaults.AuthenticationScheme, configure);

    /// <summary>
    /// Adds DPoP to a JWT bearer scheme. The scheme accepts <c>Authorization: DPoP</c> with a proof, rejects
    /// DPoP-bound tokens sent as bearer tokens and announces both schemes in its challenges.
    /// </summary>
    /// <param name="builder">The authentication builder, after <c>AddJwtBearer</c>.</param>
    /// <param name="scheme">The JWT bearer scheme.</param>
    /// <param name="configure">Sets which tokens the scheme accepts, the proof algorithms and nonces.</param>
    /// <remarks>
    /// It extends <c>options.Events</c> of the scheme; schemes that use <c>EventsType</c> are not supported. Proofs are
    /// remembered in an <see cref="IDPoPReplayCache"/>, in memory by default: register
    /// <see cref="DistributedDPoPReplayCache"/> when the API runs on several instances.
    /// </remarks>
    public static AuthenticationBuilder AddDPoP(this AuthenticationBuilder builder, string scheme, Action<DPoPJwtBearerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(scheme);

        var services = builder.Services;
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDPoPReplayCache, InMemoryDPoPReplayCache>();
        services.AddDataProtection();
        services.TryAddSingleton<DPoPNonces>();

        var settings = services.AddOptions<DPoPJwtBearerOptions>(scheme);
        if (configure != null)
        {
            settings.Configure(configure);
        }

        services.AddOptions<JwtBearerOptions>(scheme)
            .PostConfigure<IOptionsMonitor<DPoPJwtBearerOptions>>((options, monitor) => DPoPJwtBearerEvents.Attach(options, monitor.Get(scheme)));
        return builder;
    }
}
