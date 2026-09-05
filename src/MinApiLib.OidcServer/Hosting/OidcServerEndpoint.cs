namespace MinApiLib.OidcServer.Hosting;

/// <summary>
/// Turns a MinApiLib endpoint into an OidcServer protocol endpoint, and helpers for the protocol routes.
/// </summary>
/// <remarks>
/// The protocol endpoints are MinApiLib records (<c>GetHandlerAsync&lt;TRequest&gt;</c>,
/// <c>PostHandlerAsync&lt;TRequest&gt;</c>) that call <see cref="AsOidcServerEndpoint"/> from <c>Configure</c>.
/// Their request is a record struct bound with <c>[AsParameters]</c>: it carries the <see cref="HttpContext"/> and,
/// marked with <c>[FromServices]</c>, the services the endpoint needs for that request.
/// </remarks>
public static class OidcServerEndpoint
{
    /// <summary>
    /// Makes the endpoint an OidcServer protocol endpoint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The endpoint answers 404 when <see cref="Configuration.EndpointsOptions"/> disables it, or when it is a
    /// <c>/connect/mtls/...</c> alias and mutual TLS does not use the path mode.
    /// </para>
    /// <para>
    /// An exception thrown while processing the request or executing its result raises an
    /// <see cref="UnhandledExceptionEvent"/> and is rethrown.
    /// </para>
    /// <para>
    /// It adds <see cref="OidcServerEndpointMetadata"/>, used by <c>ForEndpoint</c>, and excludes the endpoint from the
    /// API description.
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder of the endpoint.</param>
    /// <param name="name">The endpoint name, see <see cref="OidcServerConstants.EndpointNames"/>.</param>
    /// <param name="path">The route pattern of the endpoint.</param>
    /// <param name="endpointType">The endpoint record type.</param>
    public static RouteHandlerBuilder AsOidcServerEndpoint(this RouteHandlerBuilder builder, string name, string path, Type endpointType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(endpointType);

        var isMutualTlsAlias = IsMutualTlsPath(path);

        return builder
            .WithDisplayName($"OidcServer {name} {path}")
            .WithMetadata(new OidcServerEndpointMetadata(name, endpointType, isMutualTlsAlias))
            .ExcludeFromDescription()
            .AddEndpointFilter((context, next) => InvokeAsync(context, next, name, endpointType, isMutualTlsAlias));
    }

    /// <summary>
    /// Returns the route of a protocol path, such as <c>/connect/token</c> for <c>connect/token</c>.
    /// </summary>
    /// <param name="path">A path of <see cref="Constants.ProtocolRoutePaths"/>.</param>
    public static string Route(string path) => path.EnsureLeadingSlash();

    /// <summary>
    /// Returns the <c>/connect/mtls/...</c> alias of a <c>connect/...</c> protocol path.
    /// </summary>
    /// <param name="path">A path of <see cref="Constants.ProtocolRoutePaths"/> under <c>connect/</c>.</param>
    public static string MutualTlsRoute(string path)
        => Constants.ProtocolRoutePaths.MtlsPathPrefix.EnsureLeadingSlash()
            + path[Constants.ProtocolRoutePaths.ConnectPathPrefix.Length..];

    internal static bool IsMutualTlsPath(string path)
        => path.StartsWith(Constants.ProtocolRoutePaths.MtlsPathPrefix.EnsureLeadingSlash() + "/", StringComparison.OrdinalIgnoreCase);

    private static async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext invocation,
        EndpointFilterDelegate next,
        string name,
        Type endpointType,
        bool isMutualTlsAlias)
    {
        var context = invocation.HttpContext;
        var options = context.RequestServices.GetRequiredService<OidcServerOptions>();
        var logger = context.RequestServices.GetRequiredService<ILogger<OidcServerMiddleware>>();

        var mtlsPathMode = options.MutualTls.Enabled && options.MutualTls.DomainName.IsMissing();
        if (!options.Endpoints.IsEndpointEnabled(name) || (isMutualTlsAlias && !mtlsPathMode))
        {
            SecretLog.EndpointDisabled(logger, name);
            return Results.NotFound();
        }

        object? result;
        try
        {
            ResponseAndHostingLog.InvokingEndpoint(logger, endpointType.FullName!, context.Request.Path.ToString());
            result = await next(invocation);
        }
        catch (Exception ex)
        {
            await OnUnhandledExceptionAsync(context, ex, logger);
            throw;
        }

        if (result is IEndpointResult endpointResult)
        {
            ResponseAndHostingLog.InvokingResult(logger, endpointResult.GetType().FullName!);
            return new GuardedResult(endpointResult, logger);
        }

        return result;
    }

    private static async Task OnUnhandledExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        await context.RequestServices.GetRequiredService<IEventService>().RaiseAsync(new UnhandledExceptionEvent(exception));
        ResponseAndHostingLog.UnhandledException(logger, exception, exception.Message);
    }

    /// <summary>
    /// Executes an endpoint result and reports the exceptions it throws.
    /// </summary>
    private sealed class GuardedResult : IResult
    {
        private readonly IEndpointResult _inner;
        private readonly ILogger _logger;

        public GuardedResult(IEndpointResult inner, ILogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            try
            {
                await _inner.ExecuteAsync(httpContext);
            }
            catch (Exception ex)
            {
                await OnUnhandledExceptionAsync(httpContext, ex, _logger);
                throw;
            }
        }
    }
}
