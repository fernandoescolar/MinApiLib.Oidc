namespace MinApiLib.Oidc.Server.Features.Interaction;

[RegisterInIServiceCollection(typeof(IReturnUrlParser))]
internal class OidcReturnUrlParser : IReturnUrlParser
{
    private readonly IAuthorizeRequestValidator _validator;
    private readonly IUserSession _userSession;
    private readonly ILogger _logger;
    private readonly IAuthorizationParametersMessageStore? _authorizationParametersMessageStore;

    public OidcReturnUrlParser(
        IAuthorizeRequestValidator validator,
        IUserSession userSession,
        ILogger<OidcReturnUrlParser> logger,
        IAuthorizationParametersMessageStore? authorizationParametersMessageStore = null)
    {
        _validator = validator;
        _userSession = userSession;
        _logger = logger;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    public async Task<AuthorizationRequest?> ParseAsync(string returnUrl)
    {
        if (IsValidReturnUrl(returnUrl))
        {
            var parameters = returnUrl.ReadQueryStringAsNameValueCollection();
            if (_authorizationParametersMessageStore != null)
            {
                var messageStoreId = parameters[Constants.AuthorizationParamsStore.MessageStoreIdParameterName];
                var entry = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                parameters = entry?.Data.FromFullDictionary() ?? new NameValueCollection();
            }

            var user = await _userSession.GetUserAsync();
            var result = await _validator.ValidateAsync(parameters, user);
            if (!result.IsError)
            {
                ServicesLog.AuthorizationRequestReturned(_logger);
                return new AuthorizationRequest(result.ValidatedRequest);
            }
        }

        ServicesLog.NoAuthorizationRequestReturned(_logger);
        return null;
    }

    public bool IsValidReturnUrl(string returnUrl)
    {
        if (returnUrl.IsLocalUrl())
        {
            var index = returnUrl.IndexOf('?');
            if (index >= 0)
            {
                returnUrl = returnUrl.Substring(0, index);
            }

            if (returnUrl.EndsWith(Constants.ProtocolRoutePaths.Authorize, StringComparison.Ordinal) ||
                returnUrl.EndsWith(Constants.ProtocolRoutePaths.AuthorizeCallback, StringComparison.Ordinal))
            {
                ServicesLog.ReturnUrlValid(_logger);
                return true;
            }
        }

        ServicesLog.ReturnUrlNotValid(_logger);
        return false;
    }
}
