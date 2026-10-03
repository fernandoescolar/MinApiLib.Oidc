namespace MinApiLib.Oidc.Server.Core.Configuration;

/// <summary>
/// OidcServer helper class for DI configuration
/// </summary>
public class OidcServerBuilder : IOidcServerBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OidcServerBuilder"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <exception cref="System.ArgumentNullException">services</exception>
    public OidcServerBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Gets the services.
    /// </summary>
    /// <value>
    /// The services.
    /// </value>
    public IServiceCollection Services { get; }
}
