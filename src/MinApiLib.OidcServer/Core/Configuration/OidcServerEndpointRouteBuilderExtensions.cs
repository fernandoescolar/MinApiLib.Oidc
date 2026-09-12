using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using MinApiLib.Endpoints;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Maps the OidcServer protocol endpoints.
/// </summary>
/// <remarks>
/// Every protocol endpoint is a MinApiLib endpoint record of this assembly, mapped with MinApiLib's
/// <c>MapEndpoints</c>. <c>UseOidcServer()</c> is required as well: it keeps the session cookie in sync, runs CORS,
/// authentication and mutual TLS, and sends logout notifications. Mapping lets the host apply per-endpoint policies
/// such as rate limiting:
/// <code>
/// app.UseOidcServer();
/// app.MapOidcServer().ForEndpoint(OidcServerConstants.EndpointNames.Token).RequireRateLimiting("token");
/// </code>
/// Custom endpoints are MinApiLib endpoints of the host's assembly, mapped with <c>app.MapEndpoints()</c>. Calling
/// <see cref="MinApiLib.OidcServer.Core.Hosting.OidcServerEndpoint.AsOidcServerEndpoint"/> from their <c>Configure</c>
/// gives them the same events and logging as the protocol endpoints.
/// </remarks>
public static class OidcServerEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all OidcServer endpoints, plus their <c>/connect/mtls/...</c> aliases.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder to add conventions to all endpoints or, with <c>ForEndpoint</c>, to one of them.</returns>
    [RequiresUnreferencedCode("MinApiLib's MapEndpoints finds the endpoint records by reflection.")]
    public static OidcServerEndpointConventionBuilder MapOidcServer(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var marker = endpoints.ServiceProvider.GetService<OidcServerEndpointsMarker>()
            ?? throw new InvalidOperationException("OidcServer services are not registered. Call services.AddOidcServer() first.");
        marker.Mapped = true;

        var group = endpoints.MapEndpoints(typeof(OidcServerEndpoint).Assembly);

        return new OidcServerEndpointConventionBuilder(group);
    }
}
