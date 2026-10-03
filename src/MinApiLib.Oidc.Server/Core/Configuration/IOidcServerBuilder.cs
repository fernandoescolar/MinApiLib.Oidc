namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OidcServer builder Interface
/// </summary>
public interface IOidcServerBuilder
{
    /// <summary>
    /// Gets the services.
    /// </summary>
    /// <value>
    /// The services.
    /// </value>
    IServiceCollection Services { get; }
}
