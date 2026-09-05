namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validates <c>authorization_code</c> token requests, including PKCE and replay detection.
/// </summary>
internal sealed class AuthorizationCodeGrantValidator : ITokenRequestGrantValidator
{
    private readonly IAuthorizationCodeStore _authorizationCodeStore;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IReferenceTokenStore _referenceTokenStore;
    private readonly IProfileService _profile;
    private readonly IResourceValidator _resourceValidator;
    private readonly TimeProvider _clock;

    public AuthorizationCodeGrantValidator(
        IAuthorizationCodeStore authorizationCodeStore,
        IRefreshTokenStore refreshTokenStore,
        IReferenceTokenStore referenceTokenStore,
        IProfileService profile,
        IResourceValidator resourceValidator,
        TimeProvider clock)
    {
        _authorizationCodeStore = authorizationCodeStore;
        _refreshTokenStore = refreshTokenStore;
        _referenceTokenStore = referenceTokenStore;
        _profile = profile;
        _resourceValidator = resourceValidator;
        _clock = clock;
    }

    public async Task<TokenRequestValidationResult> ValidateAsync(TokenRequestValidationContext context)
    {
        var request = context.Request;

        RequestValidationLog.StartAuthorizationCodeValidation(context.Logger);

        if (!request.Client.AllowedGrantTypes.ToList().Contains(GrantType.AuthorizationCode) &&
            !request.Client.AllowedGrantTypes.ToList().Contains(GrantType.Hybrid))
        {
            context.LogError("Client not authorized for code flow");
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        var code = context.Parameters.Get(OidcConstants.TokenRequest.Code);
        if (code.IsMissing())
        {
            context.LogError("Authorization code is missing");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (code.Length > context.Options.InputLengthRestrictions.AuthorizationCode)
        {
            context.LogError("Authorization code is too long");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        request.AuthorizationCodeHandle = code;

        var authZcode = await _authorizationCodeStore.GetAuthorizationCodeAsync(code);
        if (authZcode == null)
        {
            context.LogError("Invalid authorization code", new { code = code.Obfuscate() });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (authZcode.ClientId != request.Client.ClientId)
        {
            context.LogError("Client is trying to use a code from a different client", new { clientId = request.Client.ClientId, codeClient = authZcode.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (authZcode.ConsumedTime.HasValue)
        {
            // RFC 6749 section 4.1.2: deny the request and revoke tokens previously issued based on this code.
            context.LogError("Authorization code has already been redeemed. Revoking tokens issued to this subject and client.", new { clientId = request.Client.ClientId });
            await RevokeTokensForReplayedCodeAsync(code, authZcode);
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        authZcode.ConsumedTime = _clock.GetUtcNow().UtcDateTime;
        await _authorizationCodeStore.ConsumeAuthorizationCodeAsync(code, authZcode);

        if (authZcode.CreationTime.HasExceeded(authZcode.Lifetime, _clock.GetUtcNow().UtcDateTime))
        {
            context.LogError("Authorization code expired", new { code = code.Obfuscate() });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (authZcode.SessionId.IsPresent())
        {
            request.SessionId = authZcode.SessionId;
        }

        if (authZcode.CreationTime.HasExceeded(request.Client.AuthorizationCodeLifetime, _clock.GetUtcNow().UtcDateTime))
        {
            context.LogError("Authorization code is expired");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        request.AuthorizationCode = authZcode;
        request.Subject = authZcode.Subject;

        var redirectUri = context.Parameters.Get(OidcConstants.TokenRequest.RedirectUri);
        if (redirectUri.IsMissing())
        {
            context.LogError("Redirect URI is missing");
            return context.Invalid(OidcConstants.TokenErrors.UnauthorizedClient);
        }

        if (redirectUri.Equals(request.AuthorizationCode.RedirectUri, StringComparison.Ordinal) == false)
        {
            context.LogError("Invalid redirect_uri", new { redirectUri, expectedRedirectUri = request.AuthorizationCode.RedirectUri });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (request.AuthorizationCode.RequestedScopes == null ||
            !request.AuthorizationCode.RequestedScopes.Any())
        {
            context.LogError("Authorization code has no associated scopes");
            return context.Invalid(OidcConstants.TokenErrors.InvalidRequest);
        }

        var codeVerifier = context.Parameters.Get(OidcConstants.TokenRequest.CodeVerifier);
        if (request.Client.RequirePkce || request.AuthorizationCode.CodeChallenge.IsPresent())
        {
            RequestValidationLog.StartPkceValidation(context.Logger);

            var proofKeyResult = ValidateProofKey(context, codeVerifier, request.AuthorizationCode);
            if (proofKeyResult.IsError)
            {
                return proofKeyResult;
            }

            request.CodeVerifier = codeVerifier;
        }
        else
        {
            if (codeVerifier.IsPresent())
            {
                context.LogError("Unexpected code_verifier. This happens when the client is trying to use PKCE, but it is not enabled. Set RequirePkce to true.");
                return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }
        }

        // RFC 9449 section 10: a code bound with dpop_jkt can only be redeemed with a proof of that key.
        if (request.AuthorizationCode.DPoPKeyThumbprint.IsPresent() &&
            (request.DPoPKeyThumbprint == null || !FixedTimeComparer.IsEqual(request.DPoPKeyThumbprint, request.AuthorizationCode.DPoPKeyThumbprint)))
        {
            context.LogError("DPoP proof key does not match the dpop_jkt of the authorization code");
            return context.Invalid(OidcConstants.TokenErrors.InvalidDPoPProof);
        }

        var resourceResult = await ValidateResourceIndicatorAsync(context, request.AuthorizationCode);
        if (resourceResult != null)
        {
            return resourceResult;
        }

        var isActiveCtx = new IsActiveContext(request.AuthorizationCode.Subject, request.Client, OidcServerConstants.ProfileIsActiveCallers.AuthorizationCodeValidation);
        await _profile.IsActiveAsync(isActiveCtx);

        if (isActiveCtx.IsActive == false)
        {
            context.LogError("User has been disabled", new { subjectId = request.AuthorizationCode.Subject.GetSubjectId() });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        RequestValidationLog.AuthorizationCodeValidationSuccess(context.Logger);

        return context.Valid();
    }

    /// <summary>
    /// RFC 8707 section 2.2: a <c>resource</c> parameter at the token endpoint selects the audience of the access
    /// token. It must be one of the resources requested at the authorize endpoint, or, when none were, an API
    /// resource of the granted scopes.
    /// </summary>
    private async Task<TokenRequestValidationResult?> ValidateResourceIndicatorAsync(TokenRequestValidationContext context, AuthorizationCode authorizationCode)
    {
        var request = context.Request;
        var codeIndicators = authorizationCode.RequestedResourceIndicators?.ToArray() ?? [];

        var error = ResourceIndicators.TryRead(context.Parameters, context.Options.InputLengthRestrictions, allowMultiple: false, out var requested);
        if (error != null)
        {
            context.LogError(error);
            return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
        }

        if (requested.Count == 0)
        {
            request.RequestedResourceIndicators = codeIndicators;
            return null;
        }

        if (codeIndicators.Length > 0)
        {
            if (!codeIndicators.Contains(requested[0], StringComparer.Ordinal))
            {
                context.LogError("Resource indicator was not requested at the authorize endpoint", new { resource = requested[0] });
                return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
            }
        }
        else
        {
            var validation = await _resourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest
            {
                Client = request.Client,
                Scopes = authorizationCode.RequestedScopes,
                ResourceIndicators = requested
            });

            if (validation.InvalidResourceIndicators.Count > 0)
            {
                context.LogError("Resource indicator does not match an API resource of the granted scopes", new { resource = requested[0] });
                return context.Invalid(OidcConstants.TokenErrors.InvalidTarget);
            }

            if (!validation.Succeeded)
            {
                context.LogError("Authorization code scopes are no longer valid for the client");
                return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }
        }

        request.RequestedResourceIndicators = requested;
        return null;
    }

    private async Task RevokeTokensForReplayedCodeAsync(string code, AuthorizationCode authorizationCode)
    {
        var subjectId = authorizationCode.Subject.GetSubjectId();

        await _refreshTokenStore.RemoveRefreshTokensAsync(subjectId, authorizationCode.ClientId);
        await _referenceTokenStore.RemoveReferenceTokensAsync(subjectId, authorizationCode.ClientId);
        await _authorizationCodeStore.RemoveAuthorizationCodeAsync(code);
    }

    private static TokenRequestValidationResult ValidateProofKey(TokenRequestValidationContext context, string? codeVerifier, AuthorizationCode authZcode)
    {
        var request = context.Request;

        if (authZcode.CodeChallenge.IsMissing() || authZcode.CodeChallengeMethod.IsMissing())
        {
            context.LogError("Client is missing code challenge or code challenge method", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (codeVerifier.IsMissing())
        {
            context.LogError("Missing code_verifier");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (codeVerifier.Length < context.Options.InputLengthRestrictions.CodeVerifierMinLength ||
            codeVerifier.Length > context.Options.InputLengthRestrictions.CodeVerifierMaxLength)
        {
            context.LogError("code_verifier is too short or too long");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (Constants.SupportedCodeChallengeMethods.Contains(authZcode.CodeChallengeMethod) == false)
        {
            context.LogError("Unsupported code challenge method", new { codeChallengeMethod = authZcode.CodeChallengeMethod });
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        if (ValidateCodeVerifierAgainstCodeChallenge(codeVerifier, authZcode.CodeChallenge, authZcode.CodeChallengeMethod) == false)
        {
            context.LogError("Transformed code verifier does not match code challenge");
            return context.Invalid(OidcConstants.TokenErrors.InvalidGrant);
        }

        return context.Valid();
    }

    private static bool ValidateCodeVerifierAgainstCodeChallenge(string codeVerifier, string codeChallenge, string codeChallengeMethod)
    {
        if (codeChallengeMethod == OidcConstants.CodeChallengeMethods.Plain)
        {
            return FixedTimeComparer.IsEqual(codeVerifier.Sha256(), codeChallenge);
        }

        var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hashedBytes = codeVerifierBytes.Sha256();
        var transformedCodeVerifier = Base64Url.EncodeToString(hashedBytes);

        return FixedTimeComparer.IsEqual(transformedCodeVerifier.Sha256(), codeChallenge);
    }
}
