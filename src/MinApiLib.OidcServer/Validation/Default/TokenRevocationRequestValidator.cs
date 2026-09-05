namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// The token revocation request validator
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Validation.ITokenRevocationRequestValidator" />
[RegisterInIServiceCollection(typeof(ITokenRevocationRequestValidator))]
internal class TokenRevocationRequestValidator : ITokenRevocationRequestValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenRevocationRequestValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TokenRevocationRequestValidator(ILogger<TokenRevocationRequestValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="client">The client.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException">
    /// parameters
    /// or
    /// client
    /// </exception>
    public Task<TokenRevocationRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, Client client)
    {
        RequestValidationLog.RevocationValidateRequestCalled(_logger);

        if (parameters == null)
        {
            RequestValidationLog.RevocationNoParameters(_logger);
            throw new ArgumentNullException(nameof(parameters));
        }

        if (client == null)
        {
            RequestValidationLog.RevocationNoClient(_logger);
            throw new ArgumentNullException(nameof(client));
        }

        var token = parameters.Get("token");
        if (token.IsMissing())
        {
            RequestValidationLog.RevocationNoToken(_logger);
            return Task.FromResult(new TokenRevocationRequestValidationResult
            {
                IsError = true,
                Error = OidcConstants.TokenErrors.InvalidRequest
            });
        }

        var result = new TokenRevocationRequestValidationResult
        {
            IsError = false,
            Token = token,
            Client = client
        };

        var hint = parameters.Get("token_type_hint");
        if (hint.IsPresent())
        {
            if (Constants.SupportedTokenTypeHints.Contains(hint))
            {
                RequestValidationLog.TokenTypeHintFound(_logger, hint);
                result.TokenTypeHint = hint;
            }
            else
            {
                RequestValidationLog.InvalidTokenTypeHint(_logger, hint);
                result.IsError = true;
                result.Error = Constants.RevocationErrors.UnsupportedTokenType;
            }
        }

        RequestValidationLog.RevocationValidationResult(_logger, result);

        return Task.FromResult(result);
    }
}
