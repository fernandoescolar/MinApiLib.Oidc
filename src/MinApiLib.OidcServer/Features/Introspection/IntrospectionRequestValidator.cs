namespace MinApiLib.OidcServer.Features.Introspection;

/// <summary>
/// The introspection request validator
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Features.Introspection.IIntrospectionRequestValidator" />
[RegisterInIServiceCollection(typeof(IIntrospectionRequestValidator))]
internal class IntrospectionRequestValidator : IIntrospectionRequestValidator
{
    private readonly ILogger _logger;
    private readonly ITokenValidator _tokenValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntrospectionRequestValidator"/> class.
    /// </summary>
    /// <param name="tokenValidator">The token validator.</param>
    /// <param name="logger">The logger.</param>
    public IntrospectionRequestValidator(ITokenValidator tokenValidator, ILogger<IntrospectionRequestValidator> logger)
    {
        _tokenValidator = tokenValidator;
        _logger = logger;
    }

    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="api">The API.</param>
    /// <returns></returns>
    public async Task<IntrospectionRequestValidationResult> ValidateAsync(NameValueCollection parameters, ApiResource api)
    {
        RequestValidationLog.IntrospectionValidationStarted(_logger);

        var token = parameters.Get("token");
        if (token == null)
        {
            RequestValidationLog.IntrospectionTokenMissing(_logger);

            return new IntrospectionRequestValidationResult
            {
                IsError = true,
                Api = api,
                Error = "missing_token",
                Parameters = parameters
            };
        }

        var tokenValidationResult = await _tokenValidator.ValidateAccessTokenAsync(token);

        if (tokenValidationResult.IsError)
        {
            RequestValidationLog.IntrospectionTokenInvalid(_logger);

            return new IntrospectionRequestValidationResult
            {
                IsActive = false,
                IsError = false,
                Token = token,
                Api = api,
                Parameters = parameters
            };
        }

        RequestValidationLog.IntrospectionValidationSuccess(_logger);

        return new IntrospectionRequestValidationResult
        {
            IsActive = true,
            IsError = false,
            Token = token,
            Claims = tokenValidationResult.Claims,
            Api = api,
            Parameters = parameters
        };
    }
}
