namespace MinApiLib.OidcServer.Features.Tokens.Grants;

/// <summary>
/// Validates <c>password</c> token requests (resource owner password credentials, deprecated by OAuth 2.1).
/// </summary>
internal sealed class ResourceOwnerPasswordGrantValidator : ITokenRequestGrantValidator
{
    private readonly IResourceOwnerPasswordValidator _resourceOwnerValidator;
    private readonly TokenRequestScopeValidator _scopeValidator;
    private readonly IProfileService _profile;
    private readonly IEventService _events;

    public ResourceOwnerPasswordGrantValidator(IResourceOwnerPasswordValidator resourceOwnerValidator, TokenRequestScopeValidator scopeValidator, IProfileService profile, IEventService events)
    {
        _resourceOwnerValidator = resourceOwnerValidator;
        _scopeValidator = scopeValidator;
        _profile = profile;
        _events = events;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartResourceOwnerPasswordValidation(context.Logger);

        if (!request.Client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword))
        {
            context.LogError("Client not authorized for resource owner flow, check the AllowedGrantTypes setting", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        if (!request.Client.AllowResourceOwnerPasswordFlow)
        {
            context.LogError("Client requested the deprecated password grant, but AllowResourceOwnerPasswordFlow is not set", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        var scopeError = await _scopeValidator.ValidateAsync(context);
        if (scopeError != null)
        {
            return context.Invalid(scopeError);
        }

        var userName = context.Parameters.Get(OidcConstants.TokenRequest.UserName);
        var password = context.Parameters.Get(OidcConstants.TokenRequest.Password) ?? string.Empty;

        if (userName.IsMissing())
        {
            context.LogError("Username is missing");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (userName.Length > context.Options.InputLengthRestrictions.UserName ||
            password.Length > context.Options.InputLengthRestrictions.Password)
        {
            context.LogError("Username or password too long");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        request.UserName = userName;

        var resourceOwnerContext = new ResourceOwnerPasswordValidationContext
        {
            UserName = userName,
            Password = password,
            Request = request
        };
        await _resourceOwnerValidator.ValidateAsync(resourceOwnerContext);

        var result = resourceOwnerContext.Result;
        if (result.IsError)
        {
            // protect against validators that set no error
            var error = result.Error ?? OidcConstants.TokenErrors.InvalidGrant;

            if (error == OidcConstants.TokenErrors.UnsupportedGrantType)
            {
                context.LogError("Resource owner password credential grant type not supported");
                await RaiseFailureEventAsync(userName, "password grant type not supported", request.Client.ClientId);

                return context.Invalid(OidcConstants.TokenErrors.UnsupportedGrantType, customResponse: result.CustomResponse);
            }

            var errorDescription = result.ErrorDescription.IsPresent() ? result.ErrorDescription : "invalid_username_or_password";

            context.LogWarning("User authentication failed", new { error = errorDescription });
            await RaiseFailureEventAsync(userName, errorDescription, request.Client.ClientId);

            return context.Invalid(error, errorDescription, result.CustomResponse);
        }

        if (result.Subject == null)
        {
            const string NoPrincipal = "User authentication failed: no principal returned";
            context.LogError(NoPrincipal);
            await RaiseFailureEventAsync(userName, NoPrincipal, request.Client.ClientId);

            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        var isActiveCtx = new IsActiveContext(result.Subject, request.Client, OidcServerConstants.ProfileIsActiveCallers.ResourceOwnerValidation);
        await _profile.IsActiveAsync(isActiveCtx);

        if (!isActiveCtx.IsActive)
        {
            context.LogError("User has been disabled", new { subjectId = result.Subject.GetSubjectId() });
            await RaiseFailureEventAsync(userName, "user is inactive", request.Client.ClientId);

            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        request.Subject = result.Subject;

        await _events.RaiseAsync(new UserLoginSuccessEvent(userName, result.Subject.GetSubjectId(), userName, interactive: false, request.Client.ClientId));
        RequestValidationLog.ResourceOwnerPasswordValidationSuccess(context.Logger);

        return context.Valid(result.CustomResponse);
    }

    private Task RaiseFailureEventAsync(string userName, string error, string clientId)
        => _events.RaiseAsync(new UserLoginFailureEvent(userName, error, interactive: false, clientId));
}
