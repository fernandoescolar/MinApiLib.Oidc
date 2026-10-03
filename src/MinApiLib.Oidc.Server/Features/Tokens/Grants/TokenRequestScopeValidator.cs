namespace MinApiLib.Oidc.Server.Features.Tokens.Grants;

/// <summary>
/// Validates the scope parameter of token requests that request new scopes (client credentials and extension grants).
/// </summary>
internal sealed class TokenRequestScopeValidator
{
    private readonly IResourceStore _resourceStore;
    private readonly IResourceValidator _resourceValidator;

    public TokenRequestScopeValidator(IResourceStore resourceStore, IResourceValidator resourceValidator)
    {
        _resourceStore = resourceStore;
        _resourceValidator = resourceValidator;
    }

    /// <returns><c>null</c> when the scopes are valid, otherwise the OAuth error code to return.</returns>
    public async Task<string?> ValidateAsync(TokenRequestValidationContext context, bool ignoreImplicitIdentityScopes = false, bool ignoreImplicitOfflineAccess = false)
    {
        var request = context.Request;

        var scopes = context.Parameters.Get(OidcConstants.TokenRequest.Scope);
        if (scopes.IsMissing())
        {
            RequestValidationLog.NoScopesProvided(context.Logger);

            if (request.Client.AllowedScopes != null && request.Client.AllowedScopes.Count > 0)
            {
                var clientAllowedScopes = new List<string>();
                if (!ignoreImplicitIdentityScopes)
                {
                    var resources = await _resourceStore.FindResourcesByScopeAsync(request.Client.AllowedScopes);
                    clientAllowedScopes.AddRange(resources.ToScopeNames().Where(x => request.Client.AllowedScopes.Contains(x)));
                }
                else
                {
                    var apiScopes = await _resourceStore.FindApiScopesByNameAsync(request.Client.AllowedScopes);
                    clientAllowedScopes.AddRange(apiScopes.Select(x => x.Name));
                }

                if (!ignoreImplicitOfflineAccess)
                {
                    if (request.Client.AllowOfflineAccess)
                    {
                        clientAllowedScopes.Add(OidcServerConstants.StandardScopes.OfflineAccess);
                    }
                }

                scopes = clientAllowedScopes.Distinct().ToSpaceSeparatedString();
                RequestValidationLog.DefaultingToScopes(context.Logger, scopes);
            }
            else
            {
                context.LogError("No allowed scopes configured for client", new { clientId = request.Client.ClientId });
                return OidcConstants.TokenErrors.InvalidScope;
            }
        }

        if (scopes.Length > context.Options.InputLengthRestrictions.Scope)
        {
            context.LogError("Scope parameter exceeds max allowed length");
            return OidcConstants.TokenErrors.InvalidScope;
        }

        var requestedScopes = scopes.ParseScopesString();

        if (requestedScopes == null)
        {
            context.LogError("No scopes found in request");
            return OidcConstants.TokenErrors.InvalidScope;
        }

        var resourceError = ResourceIndicators.TryRead(context.Parameters, context.Options.InputLengthRestrictions, allowMultiple: false, out var resourceIndicators);
        if (resourceError != null)
        {
            context.LogError(resourceError);
            return OidcConstants.TokenErrors.InvalidTarget;
        }

        var resourceValidationResult = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
        {
            Client = request.Client,
            Scopes = requestedScopes,
            ResourceIndicators = resourceIndicators
        });

        if (resourceValidationResult.InvalidScopes.Count == 0 && resourceValidationResult.InvalidResourceIndicators.Count > 0)
        {
            context.LogError("Resource indicator does not match an API resource of the requested scopes");
            return OidcConstants.TokenErrors.InvalidTarget;
        }

        if (!resourceValidationResult.Succeeded)
        {
            if (resourceValidationResult.InvalidScopes.Count > 0)
            {
                context.LogError("Invalid scopes requested");
            }
            else
            {
                context.LogError("Invalid scopes for client requested");
            }

            return OidcConstants.TokenErrors.InvalidScope;
        }

        request.RequestedScopes = requestedScopes;
        request.RequestedResourceIndicators = resourceIndicators;
        request.ValidatedResources = resourceValidationResult;

        return null;
    }
}
