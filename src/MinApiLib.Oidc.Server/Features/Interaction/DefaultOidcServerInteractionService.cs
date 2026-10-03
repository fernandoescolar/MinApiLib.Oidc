namespace MinApiLib.Oidc.Server.Features.Interaction;

[RegisterInIServiceCollection(typeof(IOidcServerInteractionService))]
internal class DefaultOidcServerInteractionService : IOidcServerInteractionService
{
    private readonly TimeProvider _clock;
    private readonly IHttpContextAccessor _context;
    private readonly IMessageStore<LogoutMessage> _logoutMessageStore;
    private readonly IMessageStore<ErrorMessage> _errorMessageStore;
    private readonly IConsentMessageStore _consentMessageStore;
    private readonly IPersistedGrantService _grants;
    private readonly IUserSession _userSession;
    private readonly ILogger _logger;
    private readonly ReturnUrlParser _returnUrlParser;

    public DefaultOidcServerInteractionService(
        TimeProvider clock,
        IHttpContextAccessor context,
        IMessageStore<LogoutMessage> logoutMessageStore,
        IMessageStore<ErrorMessage> errorMessageStore,
        IConsentMessageStore consentMessageStore,
        IPersistedGrantService grants,
        IUserSession userSession,
        ReturnUrlParser returnUrlParser,
        ILogger<DefaultOidcServerInteractionService> logger)
    {
        _clock = clock;
        _context = context;
        _logoutMessageStore = logoutMessageStore;
        _errorMessageStore = errorMessageStore;
        _consentMessageStore = consentMessageStore;
        _grants = grants;
        _userSession = userSession;
        _returnUrlParser = returnUrlParser;
        _logger = logger;
    }

    public async Task<AuthorizationRequest?> GetAuthorizationContextAsync(string returnUrl)
    {
        var result = await _returnUrlParser.ParseAsync(returnUrl);

        if (result != null)
        {
            ServicesLog.AuthorizationRequestReturned(_logger);
        }
        else
        {
            ServicesLog.NoAuthorizationRequestReturned(_logger);
        }

        return result;
    }

    public async Task<LogoutRequest> GetLogoutContextAsync(string logoutId)
    {
        var msg = await _logoutMessageStore.ReadAsync(logoutId);
        // the interaction service is only used while handling a request
        var iframeUrl = await _context.HttpContext!.GetOidcServerSignoutFrameCallbackUrlAsync(msg?.Data);
        return new LogoutRequest(iframeUrl, msg?.Data);
    }

    public async Task<string?> CreateLogoutContextAsync()
    {
        var user = await _userSession.GetUserAsync();
        if (user != null)
        {
            var clientIds = await _userSession.GetClientListAsync();
            if (clientIds.Any())
            {
                var sid = await _userSession.GetSessionIdAsync();
                var msg = new Message<LogoutMessage>(new LogoutMessage
                {
                    SubjectId = user.GetSubjectId(),
                    SessionId = sid,
                    ClientIds = clientIds
                }, _clock.GetUtcNow().UtcDateTime);
                var id = await _logoutMessageStore.WriteAsync(msg);
                return id;
            }
        }

        return null;
    }

    public async Task<ErrorMessage?> GetErrorContextAsync(string errorId)
    {
        if (errorId != null)
        {
            var result = await _errorMessageStore.ReadAsync(errorId);
            var data = result?.Data;
            if (data != null)
            {
                ServicesLog.ErrorContextLoaded(_logger);
            }
            else
            {
                ServicesLog.NoErrorContextFound(_logger);
            }
            return data;
        }

        ServicesLog.NoErrorContextFound(_logger);

        return null;
    }

    public async Task GrantConsentAsync(AuthorizationRequest request, ConsentResponse consent, string? subject = null)
    {
        if (subject == null)
        {
            var user = await _userSession.GetUserAsync();
            subject = user?.GetSubjectId();
        }

        if (subject == null && consent.Granted)
        {
            throw new ArgumentNullException(nameof(subject), "User is not currently authenticated, and no subject id passed");
        }

        var consentRequest = new ConsentRequest(request, subject);
        await _consentMessageStore.WriteAsync(consentRequest.Id, new Message<ConsentResponse>(consent, _clock.GetUtcNow().UtcDateTime));
    }

    public Task DenyAuthorizationAsync(AuthorizationRequest request, AuthorizationError error, string? errorDescription = null)
    {
        var response = new ConsentResponse
        {
            Error = error,
            ErrorDescription = errorDescription
        };
        return GrantConsentAsync(request, response);
    }

    public bool IsValidReturnUrl(string returnUrl)
    {
        var result = _returnUrlParser.IsValidReturnUrl(returnUrl);

        if (result)
        {
            ServicesLog.IsValidReturnUrlTrue(_logger);
        }
        else
        {
            ServicesLog.IsValidReturnUrlFalse(_logger);
        }

        return result;
    }

    public async Task<IEnumerable<Grant>> GetAllUserGrantsAsync()
    {
        var user = await _userSession.GetUserAsync();
        if (user != null)
        {
            var subject = user.GetSubjectId();
            return await _grants.GetAllGrantsAsync(subject);
        }

        return Enumerable.Empty<Grant>();
    }

    public async Task RevokeUserConsentAsync(string clientId)
    {
        var user = await _userSession.GetUserAsync();
        if (user != null)
        {
            var subject = user.GetSubjectId();
            await _grants.RemoveAllGrantsAsync(subject, clientId);
        }
    }

    public async Task RevokeTokensForCurrentSessionAsync()
    {
        var user = await _userSession.GetUserAsync();
        if (user != null)
        {
            var subject = user.GetSubjectId();
            var sessionId = await _userSession.GetSessionIdAsync();
            await _grants.RemoveAllGrantsAsync(subject, sessionId: sessionId);
        }
    }
}
