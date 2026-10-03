namespace MinApiLib.Oidc.Server.Features.Tokens;

/// <summary>
/// Validates token requests: common checks here, grant-specific checks in one <see cref="ITokenRequestGrantValidator"/>
/// per grant type. The validator keeps no per-request state.
/// </summary>
[RegisterInIServiceCollection(typeof(ITokenRequestValidator))]
internal class TokenRequestValidator : ITokenRequestValidator
{
    private readonly OidcServerOptions _options;
    private readonly ICustomTokenRequestValidator _customRequestValidator;
    private readonly ILogger _logger;

    private readonly ITokenRequestGrantValidator _authorizationCode;
    private readonly ITokenRequestGrantValidator _clientCredentials;
    private readonly ITokenRequestGrantValidator _refreshToken;
    private readonly ITokenRequestGrantValidator _deviceCode;
    private readonly ITokenRequestGrantValidator _password;
    private readonly ITokenRequestGrantValidator _extensionGrant;

    public TokenRequestValidator(OidcServerOptions options,
        IAuthorizationCodeStore authorizationCodeStore,
        IProfileService profile,
        IDeviceCodeValidator deviceCodeValidator,
        ExtensionGrantValidator extensionGrantValidator,
        ICustomTokenRequestValidator customRequestValidator,
        IResourceValidator resourceValidator,
        IResourceOwnerPasswordValidator resourceOwnerValidator,
        IResourceStore resourceStore,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenStore refreshTokenStore,
        IReferenceTokenStore referenceTokenStore,
        IEventService events,
        TimeProvider clock,
        ILogger<TokenRequestValidator> logger)
    {
        _options = options;
        _customRequestValidator = customRequestValidator;
        _logger = logger;

        var scopeValidator = new TokenRequestScopeValidator(resourceStore, resourceValidator);
        _authorizationCode = new AuthorizationCodeGrantValidator(authorizationCodeStore, refreshTokenStore, referenceTokenStore, profile, resourceValidator, clock);
        _clientCredentials = new ClientCredentialsGrantValidator(scopeValidator);
        _refreshToken = new RefreshTokenGrantValidator(refreshTokenService, resourceValidator);
        _deviceCode = new DeviceCodeGrantValidator(deviceCodeValidator);
        _password = new ResourceOwnerPasswordGrantValidator(resourceOwnerValidator, scopeValidator, profile, events);
        _extensionGrant = new ExtensionGrantTokenRequestValidator(extensionGrantValidator, scopeValidator, profile);
    }

    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <returns></returns>
    public Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult)
        => ValidateRequestAsync(parameters, clientValidationResult, null);

    /// <inheritdoc/>
    public async Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult, DPoPProofValidationResult? dpopProof)
    {
        RequestValidationLog.StartTokenRequestValidation(_logger);

        var request = new ValidatedTokenRequest
        {
            Raw = parameters ?? throw new ArgumentNullException(nameof(parameters)),
            Options = _options
        };

        ArgumentNullException.ThrowIfNull(clientValidationResult);

        request.SetClient(clientValidationResult.Client, clientValidationResult.Secret, clientValidationResult.Confirmation);

        var context = new TokenRequestValidationContext(request, parameters, _options, _logger);

        if (dpopProof != null && !dpopProof.IsError)
        {
            // RFC 9449 section 5: the access token is bound to the proof key instead of any certificate.
            request.DPoPKeyThumbprint = dpopProof.JsonWebKeyThumbprint;
            request.Confirmation = dpopProof.Confirmation;
        }
        else if (request.Client.RequireDPoP)
        {
            context.LogError("Client requires DPoP but the request carried no DPoP proof", new { clientId = request.Client.ClientId });
            return context.Invalid(OidcConstants.TokenErrors.InvalidRequest, "DPoP proof required");
        }

        if (request.Client.ProtocolType != OidcServerConstants.ProtocolTypes.OpenIdConnect)
        {
            context.LogError("Invalid protocol type for client",
                new
                {
                    clientId = request.Client.ClientId,
                    expectedProtocolType = OidcServerConstants.ProtocolTypes.OpenIdConnect,
                    actualProtocolType = request.Client.ProtocolType
                });

            return context.Invalid(OidcConstants.TokenErrors.InvalidClient);
        }

        var grantType = parameters.Get(OidcConstants.TokenRequest.GrantType);
        if (grantType.IsMissing())
        {
            context.LogError("Grant type is missing");
            return context.Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
        }

        if (grantType.Length > context.Options.InputLengthRestrictions.GrantType)
        {
            context.LogError("Grant type is too long");
            return context.Invalid(OidcConstants.TokenErrors.UnsupportedGrantType);
        }

        request.GrantType = grantType;

        ITokenRequestGrantValidator grantValidator = grantType switch
        {
            OidcConstants.GrantTypes.AuthorizationCode => _authorizationCode,
            OidcConstants.GrantTypes.ClientCredentials => _clientCredentials,
            OidcConstants.GrantTypes.RefreshToken => _refreshToken,
            OidcConstants.GrantTypes.DeviceCode => _deviceCode,
            OidcConstants.GrantTypes.Password => _password,
            _ => _extensionGrant
        };

        return await RunValidationAsync(grantValidator, context);
    }

    private async Task<TokenRequestValidationResult> RunValidationAsync(ITokenRequestGrantValidator grantValidator, TokenRequestValidationContext context)
    {
        var result = await grantValidator.ValidateAsync(context);
        if (result.IsError)
        {
            return result;
        }

        RequestValidationLog.CallingCustomRequestValidator(_logger, _customRequestValidator.GetType().FullName!);

        var customValidationContext = new CustomTokenRequestValidationContext { Result = result };
        await _customRequestValidator.ValidateAsync(customValidationContext);

        if (customValidationContext.Result.IsError)
        {
            if (customValidationContext.Result.Error.IsPresent())
            {
                context.LogError("Custom token request validator", new { error = customValidationContext.Result.Error });
            }
            else
            {
                context.LogError("Custom token request validator error");
            }

            return customValidationContext.Result;
        }

        context.LogSuccess();
        return customValidationContext.Result;
    }
}
