
namespace MinApiLib.OidcServer.Validation;

[RegisterInIServiceCollection(typeof(INonceRequestValidator))]
internal class NonceRequestValidator : INonceRequestValidator
{
    private readonly ITokenValidator _tokenValidator;
    private readonly IProfileService _profile;
    private readonly IUserSession _userSession;
    private readonly ILogger _logger;

    public NonceRequestValidator(
        ITokenValidator tokenValidator,
        IProfileService profile,
        IUserSession userSession,
        ILogger<NonceRequestValidator> logger)
    {
        _tokenValidator = tokenValidator;
        _profile = profile;
        _userSession = userSession;
        _logger = logger;
    }

    public async Task<NonceRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
    {
        var token = parameters.Get(OidcServerConstants.NonceRequest.Token);
        if (token.IsMissing())
        {
            _logger.MissingTokenParameter();
            return InvalidToken();
        }

        var tokenResult = await _tokenValidator.ValidateAccessTokenAsync(token);
        if (tokenResult.IsError)
        {
            _logger.InvalidTokenParameter();
            return InvalidToken();
        }

        if (tokenResult.Client!.ClientId != clientValidationResult.Client.ClientId)
        {
            _logger.InvalidClient();
            return InvalidToken();
        }

        var subClaim = tokenResult.Claims.SingleOrDefault(c => c.Type == JwtClaimTypes.Subject);
        if (subClaim == null)
        {
            _logger.TokenContainsNoSubClaim();
            return InvalidToken();
        }

        var claims = tokenResult.Claims.Where(x => !Constants.Filters.ProtocolClaimsFilter.Contains(x.Type));
        var subject = Principal.Create("UserInfo", claims.ToArray());

        var isActiveContext = new IsActiveContext(subject, tokenResult.Client, OidcServerConstants.ProfileIsActiveCallers.UserInfoRequestValidation);
        await _profile.IsActiveAsync(isActiveContext);

        if (isActiveContext.IsActive == false)
        {
            _logger.UserIsNotActive(subject.GetSubjectId());
            return InvalidToken();
        }

        var sessionId = default(string);
        if (subject.IsAuthenticated())
        {
            sessionId = await _userSession.GetSessionIdAsync();
        }

        return new NonceRequestValidationResult
        {
            IsError = false,
            TokenValidationResult = tokenResult,
            Subject = subject,
            SessionId = sessionId
        };
    }

    private static NonceRequestValidationResult InvalidToken()
        => new()
        {
            IsError = true,
            Error = OidcConstants.ProtectedResourceErrors.InvalidToken
        };
}

internal static partial class NonceRequestValidatorLoggerExtensions
{
    [LoggerMessage(0x10010, LogLevel.Error, "Invalid token parameter")]
    public static partial void MissingTokenParameter(this ILogger logger);

    [LoggerMessage(0x10011, LogLevel.Error, "Invalid token parameter")]
    public static partial void InvalidTokenParameter(this ILogger logger);

    [LoggerMessage(0x10012, LogLevel.Error, "Invalid client")]
    public static partial void InvalidClient(this ILogger logger);

    [LoggerMessage(0x10013, LogLevel.Error, "Token contains no sub claim")]
    public static partial void TokenContainsNoSubClaim(this ILogger logger);

    [LoggerMessage(0x10014, LogLevel.Error, "User is not active: {sub}")]
    public static partial void UserIsNotActive(this ILogger logger, string sub);
}
