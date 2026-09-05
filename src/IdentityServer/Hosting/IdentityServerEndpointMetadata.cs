namespace IdentityServer.Hosting;

/// <summary>
/// Metadata attached to the ASP.NET Core endpoints created by <c>MapIdentityServer()</c>.
/// </summary>
public sealed class IdentityServerEndpointMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServerEndpointMetadata"/> class.
    /// </summary>
    /// <param name="name">The endpoint name, see <see cref="IdentityServerConstants.EndpointNames"/>.</param>
    /// <param name="handlerType">The <see cref="IEndpointHandler"/> type.</param>
    /// <param name="isMutualTlsAlias">Whether this is the <c>/connect/mtls/...</c> alias of the endpoint.</param>
    public IdentityServerEndpointMetadata(string name, Type handlerType, bool isMutualTlsAlias)
    {
        Name = name;
        HandlerType = handlerType;
        IsMutualTlsAlias = isMutualTlsAlias;
    }

    /// <summary>
    /// The endpoint name, see <see cref="IdentityServerConstants.EndpointNames"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The <see cref="IEndpointHandler"/> type that processes the request.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// Whether this is the <c>/connect/mtls/...</c> alias used for mutual TLS client authentication.
    /// </summary>
    public bool IsMutualTlsAlias { get; }
}
