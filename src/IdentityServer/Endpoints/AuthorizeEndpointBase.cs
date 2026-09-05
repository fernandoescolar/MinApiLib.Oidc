namespace IdentityServer.Endpoints;

internal abstract class AuthorizeEndpointBase : IEndpointHandler
{
    private readonly IAuthorizeResponseGenerator _authorizeResponseGenerator;

    private readonly IEventService _events;
    private readonly IdentityServerOptions _options;

    private readonly IAuthorizeInteractionResponseGenerator _interactionGenerator;

    private readonly IAuthorizeRequestValidator _validator;

    private readonly IPushedAuthorizationRequestStore? _pushedAuthorizationRequestStore;

    protected AuthorizeEndpointBase(
        IEventService events,
        ILogger<AuthorizeEndpointBase> logger,
        IdentityServerOptions options,
        IAuthorizeRequestValidator validator,
        IAuthorizeInteractionResponseGenerator interactionGenerator,
        IAuthorizeResponseGenerator authorizeResponseGenerator,
        IUserSession userSession,
        IPushedAuthorizationRequestStore? pushedAuthorizationRequestStore = null)
    {
        _events = events;
        _options = options;
        Logger = logger;
        _validator = validator;
        _interactionGenerator = interactionGenerator;
        _authorizeResponseGenerator = authorizeResponseGenerator;
        UserSession = userSession;
        _pushedAuthorizationRequestStore = pushedAuthorizationRequestStore;
    }

    protected ILogger Logger { get; private set; }

    protected IUserSession UserSession { get; private set; }

    public abstract Task<IEndpointResult> ProcessAsync(HttpContext context);

    internal async Task<IEndpointResult> ProcessAuthorizeRequestAsync(NameValueCollection parameters, ClaimsPrincipal? user, ConsentResponse? consent)
    {
        if (user != null)
        {
            Logger.UserInAuthorizeRequest(user.GetSubjectId());
        }
        else
        {
            Logger.NoUserInAuthorizeRequest();
        }

        var result = await _validator.ValidateAsync(parameters, user);
        if (result.IsError)
        {
            return await CreateErrorResultAsync(
                "Request validation failed",
                result.ValidatedRequest,
                result.Error,
                result.ErrorDescription);
        }

        var request = result.ValidatedRequest;
        LogRequest(request);

        var interactionResult = await _interactionGenerator.ProcessInteractionAsync(request, consent);
        if (interactionResult.IsError)
        {
            return await CreateErrorResultAsync("Interaction generator error", request, interactionResult.Error, interactionResult.ErrorDescription, false);
        }
        if (interactionResult.IsLogin)
        {
            return new LoginPageResult(request);
        }
        if (interactionResult.IsConsent)
        {
            return new ConsentPageResult(request);
        }
        if (interactionResult.IsRedirect)
        {
            return new CustomRedirectResult(request, interactionResult.RedirectUrl);
        }

        var response = await _authorizeResponseGenerator.CreateResponseAsync(request);

        await ConsumePushedAuthorizationRequestAsync(request);

        await RaiseResponseEventAsync(response);

        LogResponse(response);

        return new AuthorizeResult(response);
    }

    protected async Task<IEndpointResult> CreateErrorResultAsync(
        string logMessage,
        ValidatedAuthorizeRequest? request = null,
        string error = OidcConstants.AuthorizeErrors.ServerError,
        string? errorDescription = null,
        bool logError = true)
    {
        if (logError)
        {
            Logger.Error(logMessage);
        }

        if (request != null)
        {
            var details = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
            Logger.Information(details);
        }

        await RaiseFailureEventAsync(request, error, errorDescription);

        await ConsumePushedAuthorizationRequestAsync(request);

        return new AuthorizeResult(new AuthorizeResponse
        {
            // null only when validation failed before a request was built; the error page path reads it with ?.
            Request = request!,
            Error = error,
            ErrorDescription = errorDescription,
            SessionState = request?.GenerateSessionStateValue()
        });
    }

    // RFC 9126 section 4: a pushed request_uri is one-time use. It is kept while the user interacts (login, consent)
    // and removed once a response, successful or not, is sent to the client.
    private async Task ConsumePushedAuthorizationRequestAsync(ValidatedAuthorizeRequest? request)
    {
        if (request?.PushedAuthorizationReferenceValue != null && _pushedAuthorizationRequestStore != null)
        {
            await _pushedAuthorizationRequestStore.RemovePushedAuthorizationRequestAsync(request.PushedAuthorizationReferenceValue);
        }
    }

    private void LogRequest(ValidatedAuthorizeRequest request)
    {
        var details = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
        Logger.ValidatedAuthorizeRequest(details);
    }

    private void LogResponse(AuthorizeResponse response)
    {
        var details = new AuthorizeResponseLog(response);
        Logger.AuthorizeEndpointResponse(details);
    }

    private void LogTokens(AuthorizeResponse response)
    {
        var clientId = $"{response.Request.ClientId} ({response.Request.Client.ClientName ?? "no name set"})";
        var subjectId = response.Request.Subject.GetSubjectId();
        if (response.IdentityToken != null)
        {
            Logger.IdentityTokenIssued(clientId, subjectId, response.IdentityToken);
        }
        if (response.Code != null)
        {
            Logger.CodeIssued(clientId, subjectId, response.Code);
        }
        if (response.AccessToken != null)
        {
            Logger.AccessTokenIssued(clientId, subjectId, response.AccessToken);
        }
    }

    private Task RaiseFailureEventAsync(ValidatedAuthorizeRequest? request, string error, string? errorDescription)
    {
        return _events.RaiseAsync(new TokenIssuedFailureEvent(request, error, errorDescription));
    }

    private Task RaiseResponseEventAsync(AuthorizeResponse response)
    {
        if (!response.IsError)
        {
            LogTokens(response);
            return _events.RaiseAsync(new TokenIssuedSuccessEvent(response));
        }

        return RaiseFailureEventAsync(response.Request, response.Error, response.ErrorDescription);
    }
}
