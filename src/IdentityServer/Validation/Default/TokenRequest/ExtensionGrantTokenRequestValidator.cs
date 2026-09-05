namespace IdentityServer.Validation;

/// <summary>
/// Validates token requests for extension grant types through the registered <see cref="IExtensionGrantValidator"/>s.
/// </summary>
internal sealed class ExtensionGrantTokenRequestValidator : ITokenRequestGrantValidator
{
    private readonly ExtensionGrantValidator _extensionGrantValidator;
    private readonly TokenRequestScopeValidator _scopeValidator;
    private readonly IProfileService _profile;

    public ExtensionGrantTokenRequestValidator(ExtensionGrantValidator extensionGrantValidator, TokenRequestScopeValidator scopeValidator, IProfileService profile)
    {
        _extensionGrantValidator = extensionGrantValidator;
        _scopeValidator = scopeValidator;
        _profile = profile;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartExtensionGrantValidation(context.Logger);

        if (!request.Client.AllowedGrantTypes.Contains(request.GrantType))
        {
            context.LogError("Client does not have the custom grant type in the allowed list, therefore requested grant is not allowed", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
        }

        if (!_extensionGrantValidator.GetAvailableGrantTypes().Contains(request.GrantType, StringComparer.Ordinal))
        {
            context.LogError("No validator is registered for the grant type", new { grantType = request.GrantType });
            return context.Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
        }

        var scopeError = await _scopeValidator.ValidateAsync(context);
        if (scopeError != null)
        {
            return context.Invalid(scopeError);
        }

        var result = await _extensionGrantValidator.ValidateAsync(request);

        if (result == null)
        {
            context.LogError("Invalid extension grant");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (result.IsError)
        {
            if (result.Error.IsPresent())
            {
                context.LogError("Invalid extension grant", new { error = result.Error });
                return context.Invalid(result.Error, result.ErrorDescription, result.CustomResponse);
            }
            else
            {
                context.LogError("Invalid extension grant");
                return context.Invalid(OidcConstants.TokenErrors.InvalidGrant, customResponse: result.CustomResponse);
            }
        }

        if (result.Subject != null)
        {
            var isActiveCtx = new IsActiveContext(
                result.Subject,
                request.Client,
                IdentityServerConstants.ProfileIsActiveCallers.ExtensionGrantValidation);

            await _profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                context.LogError("User has been disabled", new { subjectId = result.Subject.GetSubjectId() });
                return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            request.Subject = result.Subject;
        }

        RequestValidationLog.ExtensionGrantValidationSuccess(context.Logger);
        return context.Valid(result.CustomResponse);
    }
}
