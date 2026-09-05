namespace IdentityServer.Endpoints;

internal class EndSessionEndpoint : IEndpointHandler
{
    private readonly IEndSessionRequestValidator _endSessionRequestValidator;

    private readonly ILogger _logger;

    private readonly IUserSession _userSession;

    public EndSessionEndpoint(
        IEndSessionRequestValidator endSessionRequestValidator,
        IUserSession userSession,
        ILogger<EndSessionEndpoint> logger)
    {
        _endSessionRequestValidator = endSessionRequestValidator;
        _userSession = userSession;
        _logger = logger;
    }

    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        NameValueCollection parameters;
        if (HttpMethods.IsGet(context.Request.Method))
        {
            parameters = context.Request.Query.AsNameValueCollection();
        }
        else if (HttpMethods.IsPost(context.Request.Method))
        {
            parameters = (await context.Request.ReadFormAsync()).AsNameValueCollection();
        }
        else
        {
            _logger.InvalidHttpMethodForEndSessionEndpoint();
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }

        var user = await _userSession.GetUserAsync();
        _logger.ProcessingSignoutRequest(user?.GetSubjectId() ?? "anonymous");

        var result = await _endSessionRequestValidator.ValidateAsync(parameters, user);

        if (result.IsError)
        {
            _logger.ErrorProcessingEndSessionRequest(result.Error);
        }
        else
        {
            _logger.SuccessValidatingEndSessionRequest(result.ValidatedRequest?.Client?.ClientId);
        }

        return new EndSessionResult(result);
    }
}
