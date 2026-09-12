namespace MinApiLib.OidcServer.Core.Hosting;

/// <summary>
/// Metadata attached to the ASP.NET Core endpoints mapped by <c>MapOidcServer()</c>.
/// </summary>
public sealed class OidcServerEndpointMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OidcServerEndpointMetadata"/> class.
    /// </summary>
    /// <param name="name">The endpoint name, see <see cref="OidcServerConstants.EndpointNames"/>.</param>
    /// <param name="endpointType">The endpoint record that serves the route.</param>
    /// <param name="isMutualTlsAlias">Whether this is the <c>/connect/mtls/...</c> alias of the endpoint.</param>
    public OidcServerEndpointMetadata(string name, Type endpointType, bool isMutualTlsAlias)
    {
        Name = name;
        EndpointType = endpointType;
        IsMutualTlsAlias = isMutualTlsAlias;
    }

    /// <summary>
    /// The endpoint name, see <see cref="OidcServerConstants.EndpointNames"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The endpoint record that serves the route.
    /// </summary>
    public Type EndpointType { get; }

    /// <summary>
    /// Whether this is the <c>/connect/mtls/...</c> alias used for mutual TLS client authentication.
    /// </summary>
    public bool IsMutualTlsAlias { get; }
}
