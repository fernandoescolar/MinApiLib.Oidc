namespace IdentityServer.Validation;

/// <summary>
/// Validates <c>refresh_token</c> token requests, including scope narrowing and re-validation against the client.
/// </summary>
internal sealed class RefreshTokenGrantValidator : ITokenRequestGrantValidator
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IResourceValidator _resourceValidator;

    public RefreshTokenGrantValidator(IRefreshTokenService refreshTokenService, IResourceValidator resourceValidator)
    {
        _refreshTokenService = refreshTokenService;
        _resourceValidator = resourceValidator;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartRefreshTokenValidation(context.Logger);

        var refreshTokenHandle = context.Parameters.Get(OidcConstants.TokenRequest.RefreshToken);
        if (refreshTokenHandle.IsMissing())
        {
            context.LogError("Refresh token is missing");
            return context.Invalid(OidcConstants.TokenErrors.InvalidRequest);
        }

        if (refreshTokenHandle.Length > context.Options.InputLengthRestrictions.RefreshToken)
        {
            context.LogError("Refresh token too long");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        var result = await _refreshTokenService.ValidateRefreshTokenAsync(refreshTokenHandle, request.Client);

        if (result.IsError)
        {
            context.LogWarning("Refresh token validation failed. aborting");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        // RFC 9449 section 5: refresh tokens of public clients are bound to the DPoP key they were issued with.
        var boundThumbprint = DPoPRequest.GetBoundThumbprint(result.RefreshToken!.AccessToken?.Confirmation);
        if (!request.Client.RequireClientSecret && boundThumbprint != null &&
            (request.DPoPKeyThumbprint == null || !FixedTimeComparer.IsEqual(request.DPoPKeyThumbprint, boundThumbprint)))
        {
            context.LogError("DPoP proof key does not match the key the refresh token is bound to");
            return context.Invalid(OidcConstants.TokenErrors.InvalidDPoPProof);
        }

        request.RefreshToken = result.RefreshToken;
        request.RefreshTokenHandle = refreshTokenHandle;
        request.Subject = result.RefreshToken.Subject;

        // RFC 6749 section 6: an optional scope parameter may narrow, but never widen, the original grant.
        var grantedScopes = result.RefreshToken.Scopes.ToList();
        var scopes = grantedScopes;

        var scopeParameter = context.Parameters.Get(OidcConstants.TokenRequest.Scope);
        if (scopeParameter.IsPresent())
        {
            if (scopeParameter.Length > context.Options.InputLengthRestrictions.Scope)
            {
                context.LogError("Scope parameter exceeds max allowed length");
                return context.Invalid(OidcConstants.TokenErrors.InvalidScope);
            }

            var requestedScopes = scopeParameter.ParseScopesString() ?? [];
            if (requestedScopes.Any(scope => !grantedScopes.Contains(scope)))
            {
                context.LogError("Requested scopes exceed the scopes granted to the refresh token", new { scope = scopeParameter });
                return context.Invalid(OidcConstants.TokenErrors.InvalidScope);
            }

            scopes = requestedScopes;
        }

        // RFC 8707 section 2.2: a resource parameter selects one of the resources the grant was authorized for.
        var authorizedIndicators = result.RefreshToken.AuthorizedResourceIndicators?.ToArray() ?? [];
        var resourceError = ResourceIndicators.TryRead(context.Parameters, context.Options.InputLengthRestrictions, allowMultiple: false, out var requestedIndicators);
        if (resourceError != null)
        {
            context.LogError(resourceError);
            return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
        }

        if (requestedIndicators.Count > 0 && authorizedIndicators.Length > 0 && !authorizedIndicators.Contains(requestedIndicators[0], StringComparer.Ordinal))
        {
            context.LogError("Resource indicator was not authorized for the refresh token", new { resource = requestedIndicators[0] });
            return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
        }

        IEnumerable<string> indicators = requestedIndicators.Count > 0 ? requestedIndicators : authorizedIndicators;

        // The client may have lost access to some scopes since the refresh token was issued.
        var resourceValidationResult = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
        {
            Client = request.Client,
            Scopes = scopes,
            ResourceIndicators = indicators
        });

        if (resourceValidationResult.InvalidScopes.Count == 0 && resourceValidationResult.InvalidResourceIndicators.Count > 0)
        {
            context.LogError("Resource indicator does not match an API resource of the granted scopes", new { resource = indicators.ToSpaceSeparatedString() });
            return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
        }

        if (!resourceValidationResult.Succeeded)
        {
            context.LogError("Refresh token scopes are no longer valid for the client", new { scopes = scopes.ToSpaceSeparatedString() });
            return context.Invalid(OidcConstants.TokenErrors.InvalidScope);
        }

        request.RequestedScopes = scopes;
        request.RequestedResourceIndicators = requestedIndicators.Count > 0 ? requestedIndicators : null;
        request.ValidatedResources = resourceValidationResult;

        RequestValidationLog.RefreshTokenValidationSuccess(context.Logger);

        return context.Valid();
    }
}
