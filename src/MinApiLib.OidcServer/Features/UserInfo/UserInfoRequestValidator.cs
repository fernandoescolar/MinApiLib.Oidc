namespace MinApiLib.OidcServer.Features.UserInfo;

/// <summary>
/// Default userinfo request validator
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Features.UserInfo.IUserInfoRequestValidator" />
[RegisterInIServiceCollection(typeof(IUserInfoRequestValidator))]
internal class UserInfoRequestValidator : IUserInfoRequestValidator
{
    private readonly ITokenValidator _tokenValidator;
    private readonly IProfileService _profile;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfoRequestValidator" /> class.
    /// </summary>
    /// <param name="tokenValidator">The token validator.</param>
    /// <param name="profile">The profile service</param>
    /// <param name="logger">The logger.</param>
    public UserInfoRequestValidator(
        ITokenValidator tokenValidator,
        IProfileService profile,
        ILogger<UserInfoRequestValidator> logger)
    {
        _tokenValidator = tokenValidator;
        _profile = profile;
        _logger = logger;
    }

    /// <summary>
    /// Validates a userinfo request.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public async Task<UserInfoRequestValidationResult> ValidateRequestAsync(string accessToken)
    {
        var tokenResult = await _tokenValidator.ValidateAccessTokenAsync(
            accessToken,
            OidcServerConstants.StandardScopes.OpenId);

        if (tokenResult.IsError)
        {
            return new UserInfoRequestValidationResult
            {
                IsError = true,
                Error = tokenResult.Error
            };
        }

        var subClaim = tokenResult.Claims.SingleOrDefault(c => c.Type == JwtClaimTypes.Subject);
        if (subClaim == null)
        {
            RequestValidationLog.UserInfoNoSubClaim(_logger);

            return new UserInfoRequestValidationResult
            {
                IsError = true,
                Error = OidcConstants.ProtectedResourceErrors.InvalidToken
            };
        }

        var claims = tokenResult.Claims.Where(x => !Constants.Filters.ProtocolClaimsFilter.Contains(x.Type));
        var subject = Principal.Create("UserInfo", claims.ToArray());

        var isActiveContext = new IsActiveContext(subject, tokenResult.Client!, OidcServerConstants.ProfileIsActiveCallers.UserInfoRequestValidation);
        await _profile.IsActiveAsync(isActiveContext);

        if (isActiveContext.IsActive == false)
        {
            RequestValidationLog.UserInfoUserNotActive(_logger, subject.GetSubjectId());

            return new UserInfoRequestValidationResult
            {
                IsError = true,
                Error = OidcConstants.ProtectedResourceErrors.InvalidToken
            };
        }

        return new UserInfoRequestValidationResult
        {
            IsError = false,
            TokenValidationResult = tokenResult,
            Subject = subject
        };
    }
}
