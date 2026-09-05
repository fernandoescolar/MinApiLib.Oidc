namespace IdentityServer.Validation;

/// <summary>
/// Validates requests to the end session endpoint.
/// </summary>
public class EndSessionRequestValidator : IEndSessionRequestValidator
{
    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    ///  The IdentityServer options.
    /// </summary>
    protected readonly IdentityServerOptions Options;

    /// <summary>
    /// The token validator.
    /// </summary>
    protected readonly ITokenValidator TokenValidator;

    /// <summary>
    /// The URI validator.
    /// </summary>
    protected readonly IRedirectUriValidator UriValidator;

    /// <summary>
    /// The user session service.
    /// </summary>
    protected readonly IUserSession UserSession;

    /// <summary>
    /// The logout notification service.
    /// </summary>
    public ILogoutNotificationService LogoutNotificationService { get; }

    /// <summary>
    /// The end session message store.
    /// </summary>
    protected readonly IMessageStore<LogoutNotificationContext> EndSessionMessageStore;

    /// <summary>
    /// The HTTP context accessor.
    /// </summary>
    protected readonly IHttpContextAccessor Context;

    /// <summary>
    /// The client store, used to resolve a <c>client_id</c> sent without an <c>id_token_hint</c>.
    /// </summary>
    protected readonly IClientStore Clients;

    /// <summary>
    /// Creates a new instance of the EndSessionRequestValidator.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <param name="tokenValidator"></param>
    /// <param name="uriValidator"></param>
    /// <param name="userSession"></param>
    /// <param name="logoutNotificationService"></param>
    /// <param name="endSessionMessageStore"></param>
    /// <param name="clients"></param>
    /// <param name="logger"></param>
    public EndSessionRequestValidator(
        IHttpContextAccessor context,
        IdentityServerOptions options,
        ITokenValidator tokenValidator,
        IRedirectUriValidator uriValidator,
        IUserSession userSession,
        ILogoutNotificationService logoutNotificationService,
        IMessageStore<LogoutNotificationContext> endSessionMessageStore,
        IClientStore clients,
        ILogger<EndSessionRequestValidator> logger)
    {
        Context = context;
        Options = options;
        TokenValidator = tokenValidator;
        UriValidator = uriValidator;
        UserSession = userSession;
        LogoutNotificationService = logoutNotificationService;
        EndSessionMessageStore = endSessionMessageStore;
        Clients = clients;
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task<EndSessionValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject)
    {
        RequestValidationLog.StartEndSessionValidation(Logger);

        var isAuthenticated = subject.IsAuthenticated();

        if (!isAuthenticated && Options.Authentication.RequireAuthenticatedUserForSignOutMessage)
        {
            return Invalid("User is anonymous. Ignoring end session parameters");
        }

        var validatedRequest = new ValidatedEndSessionRequest
        {
            Raw = parameters
        };

        var clientId = parameters.Get(OidcConstants.AuthorizeRequest.ClientId);
        if (clientId != null && clientId.Length > Options.InputLengthRestrictions.ClientId)
        {
            return Invalid("client_id is too long", validatedRequest);
        }

        var idTokenHint = parameters.Get(OidcConstants.EndSessionRequest.IdTokenHint);
        if (idTokenHint.IsPresent())
        {
            var tokenValidationResult = await TokenValidator.ValidateIdentityTokenAsync(idTokenHint, null, false);
            if (tokenValidationResult.IsError)
            {
                return Invalid("Error validating id token hint", validatedRequest);
            }

            validatedRequest.Client = tokenValidationResult.Client!;
            validatedRequest.IsIdTokenHintValidated = true;

            // OpenID Connect RP-Initiated Logout 1.0 section 2: when both are present, client_id must match the ID token.
            if (clientId.IsPresent() && !string.Equals(clientId, validatedRequest.Client?.ClientId, StringComparison.Ordinal))
            {
                return Invalid("client_id does not match the client of the id_token_hint", validatedRequest);
            }

            var subClaim = tokenValidationResult.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
            if (subClaim != null && subject.IsAuthenticated())
            {
                if (subject.GetSubjectId() != subClaim.Value)
                {
                    return Invalid("Current user does not match identity token", validatedRequest);
                }

                validatedRequest.Subject = subject;
                validatedRequest.SessionId = await UserSession.GetSessionIdAsync();
                validatedRequest.ClientIds = await UserSession.GetClientListAsync();
            }

            await ValidatePostLogoutRedirectUriAsync(parameters, validatedRequest);
        }
        else
        {
            if (clientId.IsPresent())
            {
                // OpenID Connect RP-Initiated Logout 1.0 section 2: client_id alone identifies the client so the
                // post_logout_redirect_uri can be checked. It does not authenticate the request, so the UI must still
                // ask the user to confirm (LogoutRequest.ShowSignoutPrompt).
                var client = await Clients.FindEnabledClientByIdAsync(clientId);
                if (client == null)
                {
                    return Invalid("Unknown or disabled client_id", validatedRequest);
                }

                if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.OpenIdConnect)
                {
                    return Invalid("client_id does not belong to an OpenID Connect client", validatedRequest);
                }

                validatedRequest.Client = client;
            }

            // Null for an anonymous sign-out; LogoutMessage reads it with ?.
            validatedRequest.Subject = subject!;
            validatedRequest.SessionId = await UserSession.GetSessionIdAsync();
            validatedRequest.ClientIds = await UserSession.GetClientListAsync();

            if (validatedRequest.Client != null)
            {
                await ValidatePostLogoutRedirectUriAsync(parameters, validatedRequest);
            }
        }

        LogSuccess(validatedRequest);

        return new EndSessionValidationResult
        {
            ValidatedRequest = validatedRequest,
            IsError = false
        };
    }

    private async Task ValidatePostLogoutRedirectUriAsync(NameValueCollection parameters, ValidatedEndSessionRequest validatedRequest)
    {
        var redirectUri = parameters.Get(OidcConstants.EndSessionRequest.PostLogoutRedirectUri);
        if (redirectUri.IsPresent())
        {
            if (await UriValidator.IsPostLogoutRedirectUriValidAsync(redirectUri, validatedRequest.Client))
            {
                validatedRequest.PostLogOutUri = redirectUri;
            }
            else
            {
                RequestValidationLog.InvalidPostLogoutRedirectUri(Logger, redirectUri);
            }
        }

        if (validatedRequest.PostLogOutUri != null)
        {
            var state = parameters.Get(OidcConstants.EndSessionRequest.State);
            if (state.IsPresent())
            {
                validatedRequest.State = state;
            }
        }
    }

    /// <summary>
    /// Creates a result that indicates an error.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual EndSessionValidationResult Invalid(string message, ValidatedEndSessionRequest? request = null)
    {
        message = "End session request validation failure: " + message;
        if (request != null)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                var log = new EndSessionRequestValidationLog(request);
                RequestValidationLog.EndSessionValidationFailureWithDetails(Logger, message, log);
            }
        }
        else
        {
            RequestValidationLog.EndSessionValidationFailure(Logger, message);
        }

        return new EndSessionValidationResult
        {
            IsError = true,
            Error = "Invalid request",
            ErrorDescription = message
        };
    }

    /// <summary>
    /// Logs a success result.
    /// </summary>
    /// <param name="request"></param>
    protected virtual void LogSuccess(ValidatedEndSessionRequest request)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var log = new EndSessionRequestValidationLog(request);
            RequestValidationLog.EndSessionValidationSuccess(Logger, log);
        }
    }

    /// <inheritdoc />
    public async Task<EndSessionCallbackValidationResult> ValidateCallbackAsync(NameValueCollection parameters)
    {
        var result = new EndSessionCallbackValidationResult
        {
            IsError = true
        };

        var endSessionId = parameters[Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback];
        var endSessionMessage = await EndSessionMessageStore.ReadAsync(endSessionId);
        if (endSessionMessage?.Data?.ClientIds?.Any() == true)
        {
            result.IsError = false;
            result.FrontChannelLogoutUrls = await LogoutNotificationService.GetFrontChannelLogoutNotificationsUrlsAsync(endSessionMessage.Data);
        }
        else
        {
            result.Error = "Failed to read end session callback message";
        }

        return result;
    }
}
