
namespace IdentityServer.Endpoints;

internal class NonceEndpoint : IEndpointHandler
{
    private readonly IClientSecretValidator _clientValidator;
    private readonly INonceRequestValidator _requestValidator;
    private readonly INonceResponseGenerator _responseGenerator;
    private readonly IEventService _events;
    private readonly ILogger<NonceEndpoint> _logger;

    public NonceEndpoint(
        IClientSecretValidator clientValidator,
        INonceRequestValidator requestValidator,
        INonceResponseGenerator responseGenerator,
        IEventService events,
        ILogger<NonceEndpoint> logger)
    {
        _clientValidator = clientValidator;
        _requestValidator = requestValidator;
        _responseGenerator = responseGenerator;
        _events = events;
        _logger = logger;
    }

    public Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        _logger.ProcessingNonceRequest();

        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasApplicationFormContentType())
        {
            _logger.InvalidHttpRequestForNonceEndpoint();
            return Task.FromResult<IEndpointResult>(Error(OidcConstants.TokenErrors.InvalidRequest));
        }

        return ProcessNonceRequestAsync(context);
    }

    private async Task<IEndpointResult> ProcessNonceRequestAsync(HttpContext context)
    {
        _logger.StartNonceRequest();

        var clientResult = await _clientValidator.ValidateAsync(context);
        if (clientResult.Client == null)
        {
            return Error(OidcConstants.TokenErrors.InvalidClient);
        }

        if (!clientResult.Client.AllowedGrantTypes.Contains(GrantType.NoncePattern))
        {
            return Error(OidcConstants.TokenErrors.InvalidGrant, $"client should be allowed to use grant type {GrantType.NoncePattern}");
        }

        var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
        _logger.CallingIntoNonceRequestValidator(_requestValidator.GetType().FullName);

        var requestResult = await _requestValidator.ValidateRequestAsync(form, clientResult);
        if (requestResult.IsError)
        {
            await _events.RaiseAsync(new NonceIssuedFailureEvent(requestResult));
            return Error(requestResult.Error, requestResult.ErrorDescription);
        }

        _logger.CallingIntoNonceRequestResponseGenerator(_responseGenerator.GetType().FullName);
        var response = await _responseGenerator.ProcessAsync(requestResult);

        await _events.RaiseAsync(new NonceIssuedSuccessEvent(response, requestResult));
        _logger.NonceRequestSuccess();

        return new NonceResult(response);
    }

    private static NonceErrorResult Error(string message, string? errorDescription = null)
        => new(message, errorDescription);

}
