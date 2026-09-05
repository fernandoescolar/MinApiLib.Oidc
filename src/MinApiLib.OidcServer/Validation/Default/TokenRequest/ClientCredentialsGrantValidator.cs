namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validates <c>client_credentials</c> token requests.
/// </summary>
internal sealed class ClientCredentialsGrantValidator : ITokenRequestGrantValidator
{
    private readonly TokenRequestScopeValidator _scopeValidator;

    public ClientCredentialsGrantValidator(TokenRequestScopeValidator scopeValidator)
    {
        _scopeValidator = scopeValidator;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartClientCredentialsValidation(context.Logger);

        if (!request.Client.AllowedGrantTypes.ToList().Contains(GrantType.ClientCredentials))
        {
            context.LogError("Client not authorized for client credentials flow, check the AllowedGrantTypes setting", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        var scopeError = await _scopeValidator.ValidateAsync(context, ignoreImplicitIdentityScopes: true, ignoreImplicitOfflineAccess: true);
        if (scopeError != null)
        {
            return context.Invalid(scopeError);
        }

        if (request.ValidatedResources.Resources.IdentityResources.Count > 0)
        {
            context.LogError("Client cannot request OpenID scopes in client credentials flow", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.InvalidScope);
        }

        if (request.ValidatedResources.Resources.OfflineAccess)
        {
            context.LogError("Client cannot request a refresh token in client credentials flow", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.InvalidScope);
        }

        RequestValidationLog.ClientCredentialsValidationSuccess(context.Logger, request.Client.ClientId);
        return context.Valid();
    }
}
