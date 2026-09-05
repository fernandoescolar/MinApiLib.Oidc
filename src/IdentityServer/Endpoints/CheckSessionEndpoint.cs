namespace IdentityServer.Endpoints;

internal class CheckSessionEndpoint : IEndpointHandler
{
    private readonly ILogger _logger;

    public CheckSessionEndpoint(ILogger<CheckSessionEndpoint> logger)
    {
        _logger = logger;
    }

    public Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        IEndpointResult result;

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            _logger.InvalidHttpMethodForCheckSessionEndpoint();
            result = new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }
        else
        {
            _logger.RenderingCheckSessionResult();
            result = new CheckSessionResult();
        }

        return Task.FromResult(result);
    }
}
