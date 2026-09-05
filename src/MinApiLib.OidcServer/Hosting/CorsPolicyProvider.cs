namespace MinApiLib.OidcServer.Hosting;

internal class CorsPolicyProvider : ICorsPolicyProvider
{
    private readonly ILogger _logger;
    private readonly ICorsPolicyProvider _inner;
    private readonly OidcServerOptions _options;
    private readonly IHttpContextAccessor _httpContext;

    public CorsPolicyProvider(
        ILogger<CorsPolicyProvider> logger,
        [FromKeyedServices(DecoratorServiceCollectionExtensions.InnerServiceKey)] ICorsPolicyProvider inner,
        OidcServerOptions options,
        IHttpContextAccessor httpContext)
    {
        _logger = logger;
        _inner = inner;
        _options = options;
        _httpContext = httpContext;
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (_options.Cors.CorsPolicyName == policyName)
        {
            return ProcessAsync(context);
        }
        else
        {
            return _inner.GetPolicyAsync(context, policyName);
        }
    }

    private async Task<CorsPolicy?> ProcessAsync(HttpContext context)
    {
        var origin = context.Request.GetCorsOrigin();
        if (origin != null)
        {
            var path = context.Request.Path;
            if (IsPathAllowed(path))
            {
                ResponseAndHostingLog.CorsRequest(_logger, path, origin);

                var corsPolicyService = _httpContext.HttpContext!.RequestServices.GetRequiredService<ICorsPolicyService>();

                if (await corsPolicyService.IsOriginAllowedAsync(origin))
                {
                    ResponseAndHostingLog.CorsOriginAllowed(_logger, origin);
                    return Allow(origin);
                }
                else
                {
                    ResponseAndHostingLog.CorsOriginNotAllowed(_logger, origin);
                }
            }
            else
            {
                ResponseAndHostingLog.CorsRequestIgnored(_logger, path, origin);
            }
        }

        return null;
    }

    private CorsPolicy Allow(string origin)
    {
        var policyBuilder = new CorsPolicyBuilder()
            .WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (_options.Cors.PreflightCacheDuration.HasValue)
        {
            policyBuilder.SetPreflightMaxAge(_options.Cors.PreflightCacheDuration.Value);
        }

        return policyBuilder.Build();
    }

    private bool IsPathAllowed(PathString path)
    {
        return _options.Cors.CorsPaths.Any(x => path == x);
    }
}
