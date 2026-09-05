using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Maps the IdentityServer protocol endpoints with ASP.NET Core endpoint routing.
/// </summary>
/// <remarks>
/// This is the only way the protocol endpoints are served. <c>UseIdentityServer()</c> is required as well: it keeps
/// the session cookie in sync, runs CORS, authentication and mutual TLS, and sends logout notifications. Mapping lets
/// the host apply per-endpoint policies such as rate limiting:
/// <code>
/// app.UseIdentityServer();
/// app.MapIdentityServer().ForEndpoint(IdentityServerConstants.EndpointNames.Token).RequireRateLimiting("token");
/// </code>
/// Endpoints registered with <c>AddEndpoint&lt;T&gt;</c> are mapped too. When several share a path, the first one
/// registered wins. A disabled endpoint answers 404.
/// </remarks>
public static class IdentityServerEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all registered IdentityServer endpoints, plus their <c>/connect/mtls/...</c> aliases.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder to add conventions to all endpoints or, with <c>ForEndpoint</c>, to one of them.</returns>
    public static IdentityServerEndpointConventionBuilder MapIdentityServer(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var marker = endpoints.ServiceProvider.GetService<IdentityServerEndpointsMarker>()
            ?? throw new InvalidOperationException("IdentityServer services are not registered. Call services.AddIdentityServer() first.");
        marker.Mapped = true;

        var mapped = new List<(string Name, IEndpointConventionBuilder Builder)>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in endpoints.ServiceProvider.GetServices<IdentityServer.Hosting.Endpoint>())
        {
            // endpoints are always registered with a non-empty path
            var path = descriptor.Path.Value!;
            if (!paths.Add(path))
            {
                continue;
            }

            mapped.Add((descriptor.Name, Map(endpoints, descriptor, path, isMutualTlsAlias: false)));

            var connectPrefix = Constants.ProtocolRoutePaths.ConnectPathPrefix.EnsureLeadingSlash();
            if (descriptor.Path.StartsWithSegments(connectPrefix, out var rest))
            {
                var mtlsPath = Constants.ProtocolRoutePaths.MtlsPathPrefix.EnsureLeadingSlash() + rest.Value;
                if (paths.Add(mtlsPath))
                {
                    mapped.Add((descriptor.Name, Map(endpoints, descriptor, mtlsPath, isMutualTlsAlias: true)));
                }
            }
        }

        return new IdentityServerEndpointConventionBuilder(mapped);
    }

    private static IEndpointConventionBuilder Map(IEndpointRouteBuilder endpoints, IdentityServer.Hosting.Endpoint descriptor, string pattern, bool isMutualTlsAlias)
    {
        return endpoints.Map(pattern, context => ExecuteAsync(context, descriptor, isMutualTlsAlias))
            .WithDisplayName($"IdentityServer {descriptor.Name} {pattern}")
            .WithMetadata(new IdentityServerEndpointMetadata(descriptor.Name, descriptor.Handler, isMutualTlsAlias));
    }

    private static async Task ExecuteAsync(HttpContext context, IdentityServer.Hosting.Endpoint descriptor, bool isMutualTlsAlias)
    {
        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        var logger = context.RequestServices.GetRequiredService<ILogger<IdentityServerMiddleware>>();

        var mtlsPathMode = options.MutualTls.Enabled && options.MutualTls.DomainName.IsMissing();
        if (!options.Endpoints.IsEndpointEnabled(descriptor) || (isMutualTlsAlias && !mtlsPathMode))
        {
            SecretLog.EndpointDisabled(logger, descriptor.Name);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await EndpointHandlerExecutor.ExecuteAsync(
            context,
            (IEndpointHandler)context.RequestServices.GetRequiredService(descriptor.Handler),
            logger);
    }
}
