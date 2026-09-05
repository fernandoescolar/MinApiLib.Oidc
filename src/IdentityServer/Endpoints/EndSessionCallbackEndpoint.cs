namespace IdentityServer.Endpoints;

internal class EndSessionCallbackEndpoint : IEndpointHandler
{
    private readonly IEndSessionRequestValidator _endSessionRequestValidator;
    private readonly ILogger _logger;

    public EndSessionCallbackEndpoint(
        IEndSessionRequestValidator endSessionRequestValidator,
        ILogger<EndSessionCallbackEndpoint> logger)
    {
        _endSessionRequestValidator = endSessionRequestValidator;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            _logger.InvalidHttpMethodForEndSessionCallbackEndpoint();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        _logger.ProcessingSignoutCallbackRequest();

        var parameters = context.Request.Query.AsNameValueCollection();
        var result = await _endSessionRequestValidator.ValidateCallbackAsync(parameters);

        if (!result.IsError)
        {
            _logger.SuccessfulSignoutCallback();
        }
        else
        {
            _logger.ErrorValidatingSignoutCallback(result.Error);
        }

        return new EndSessionCallbackResult(result);
    }
}
