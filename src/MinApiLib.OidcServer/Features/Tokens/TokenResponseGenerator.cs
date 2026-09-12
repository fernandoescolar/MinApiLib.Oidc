namespace MinApiLib.OidcServer.Features.Tokens;

/// <summary>
/// The default token response generator
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Features.Tokens.ITokenResponseGenerator" />
[RegisterInIServiceCollection(typeof(ITokenResponseGenerator))]
public class TokenResponseGenerator : ITokenResponseGenerator
{
    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The token service
    /// </summary>
    protected readonly ITokenService TokenService;

    /// <summary>
    /// The refresh token service
    /// </summary>
    protected readonly IRefreshTokenService RefreshTokenService;

    /// <summary>
    /// The scope parser
    /// </summary>
    public IScopeParser ScopeParser { get; }

    /// <summary>
    /// The resource store
    /// </summary>
    protected readonly IResourceStore Resources;

    /// <summary>
    /// The clients store
    /// </summary>
    protected readonly IClientStore Clients;

    /// <summary>
    ///  The clock
    /// </summary>
    protected readonly TimeProvider Clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenResponseGenerator" /> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <param name="tokenService">The token service.</param>
    /// <param name="refreshTokenService">The refresh token service.</param>
    /// <param name="scopeParser">The scope parser.</param>
    /// <param name="resources">The resources.</param>
    /// <param name="clients">The clients.</param>
    /// <param name="logger">The logger.</param>
    public TokenResponseGenerator(TimeProvider clock, ITokenService tokenService, IRefreshTokenService refreshTokenService, IScopeParser scopeParser, IResourceStore resources, IClientStore clients, ILogger<TokenResponseGenerator> logger)
    {
        Clock = clock;
        TokenService = tokenService;
        RefreshTokenService = refreshTokenService;
        ScopeParser = scopeParser;
        Resources = resources;
        Clients = clients;
        Logger = logger;
    }

    /// <summary>
    /// Processes the response.
    /// </summary>
    /// <param name="validationResult">The validationResult.</param>
    /// <returns></returns>
    public virtual async Task<TokenResponse> ProcessAsync(TokenRequestValidationResult validationResult)
    {
        var response = validationResult.ValidatedRequest.GrantType switch
        {
            OidcConstants.GrantTypes.ClientCredentials => await ProcessClientCredentialsRequestAsync(validationResult),
            OidcConstants.GrantTypes.AuthorizationCode => await ProcessAuthorizationCodeRequestAsync(validationResult),
            OidcConstants.GrantTypes.RefreshToken => await ProcessRefreshTokenRequestAsync(validationResult),
            OidcConstants.GrantTypes.DeviceCode => await ProcessDeviceCodeRequestAsync(validationResult),
            OidcConstants.GrantTypes.Password => await ProcessPasswordRequestAsync(validationResult),
            _ => await ProcessExtensionGrantRequestAsync(validationResult)
        };

        // RFC 9449 section 5: a DPoP-bound access token is of type DPoP.
        if (validationResult.ValidatedRequest.DPoPKeyThumbprint != null)
        {
            response.AccessTokenType = OidcConstants.TokenResponse.DPoPTokenType;
        }

        return response;
    }

    /// <summary>
    /// Creates the response for an client credentials request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    protected virtual Task<TokenResponse> ProcessClientCredentialsRequestAsync(TokenRequestValidationResult request)
    {
        ResponseAndHostingLog.CreatingClientCredentialsResponse(Logger);

        return ProcessTokenRequestAsync(request);
    }

    /// <summary>
    /// Creates the response for a password request (deprecated resource owner password credentials grant).
    /// </summary>
    /// <param name="request">The request.</param>
    protected virtual Task<TokenResponse> ProcessPasswordRequestAsync(TokenRequestValidationResult request)
    {
        ResponseAndHostingLog.CreatingPasswordResponse(Logger);

        return ProcessTokenRequestAsync(request);
    }

    /// <summary>
    /// Creates the response for an authorization code request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    /// <exception cref="System.InvalidOperationException">Client does not exist anymore.</exception>
    protected virtual async Task<TokenResponse> ProcessAuthorizationCodeRequestAsync(TokenRequestValidationResult request)
    {
        if (request.ValidatedRequest.AuthorizationCode is null)
        {
            throw new InvalidOperationException("An authorization code request was validated without an authorization code.");
        }

        ResponseAndHostingLog.CreatingAuthorizationCodeResponse(Logger);

        var (accessToken, refreshToken) = await CreateAccessTokenAsync(request.ValidatedRequest);
        var response = new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
            Custom = request.CustomResponse,
            Scope = request.ValidatedRequest.AuthorizationCode.RequestedScopes.ToSpaceSeparatedString()
        };

        if (refreshToken.IsPresent())
        {
            response.RefreshToken = refreshToken;
        }

        if (request.ValidatedRequest.AuthorizationCode.IsOpenId)
        {
            Client? client = null;
            if (request.ValidatedRequest.AuthorizationCode.ClientId != null)
            {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.AuthorizationCode.ClientId);
            }
            if (client == null)
            {
                throw new InvalidOperationException("Client does not exist anymore.");
            }

            var parsedScopesResult = ScopeParser.ParseScopeValues(request.ValidatedRequest.AuthorizationCode.RequestedScopes);
            var validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);

            var tokenRequest = new TokenCreationRequest
            {
                Subject = request.ValidatedRequest.AuthorizationCode.Subject,
                ValidatedResources = validatedResources,
                Nonce = request.ValidatedRequest.AuthorizationCode.Nonce,
                AccessTokenToHash = response.AccessToken,
                StateHash = request.ValidatedRequest.AuthorizationCode.StateHash,
                ValidatedRequest = request.ValidatedRequest
            };

            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
            var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            response.IdentityToken = jwt;
        }

        return response;
    }

    /// <summary>
    /// Creates the response for a refresh token request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    protected virtual async Task<TokenResponse> ProcessRefreshTokenRequestAsync(TokenRequestValidationResult request)
    {
        if (request.ValidatedRequest.RefreshToken is null || request.ValidatedRequest.RefreshTokenHandle is null)
        {
            throw new InvalidOperationException("A refresh token request was validated without a refresh token.");
        }

        ResponseAndHostingLog.CreatingRefreshTokenResponse(Logger);

        var oldAccessToken = request.ValidatedRequest.RefreshToken.AccessToken;
        string accessTokenString;
        Token? newAccessToken = null;

        // A scope parameter narrower than the original grant (RFC 6749 section 6) requires a new access token.
        var requestedScopes = request.ValidatedRequest.RequestedScopes;
        var isDownScoped = requestedScopes != null && oldAccessToken.Scopes.Any(scope => !requestedScopes.Contains(scope));

        // A resource parameter (RFC 8707) selects the audience, which also requires a new access token.
        isDownScoped |= request.ValidatedRequest.RequestedResourceIndicators?.Any() == true;

        if (request.ValidatedRequest.Client.UpdateAccessTokenClaimsOnRefresh || isDownScoped)
        {
            var subject = request.ValidatedRequest.RefreshToken.Subject;

            var validatedResources = request.ValidatedRequest.ValidatedResources;
            if (validatedResources == null)
            {
                var parsedScopesResult = ScopeParser.ParseScopeValues(oldAccessToken.Scopes);
                validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);
            }

            var creationRequest = new TokenCreationRequest
            {
                Subject = subject,
                Description = request.ValidatedRequest.RefreshToken.Description,
                ValidatedRequest = request.ValidatedRequest,
                ValidatedResources = validatedResources
            };

            newAccessToken = await TokenService.CreateAccessTokenAsync(creationRequest);
            accessTokenString = await TokenService.CreateSecurityTokenAsync(newAccessToken);
        }
        else
        {
            oldAccessToken.CreationTime = Clock.GetUtcNow().UtcDateTime;
            oldAccessToken.Lifetime = request.ValidatedRequest.AccessTokenLifetime;

            // The reused token is bound to the key of this request's DPoP proof, or loses a previous DPoP binding.
            if (request.ValidatedRequest.DPoPKeyThumbprint != null || DPoPRequest.GetBoundThumbprint(oldAccessToken.Confirmation) != null)
            {
                oldAccessToken.Confirmation = request.ValidatedRequest.Confirmation;
            }

            // Every issued access token gets a unique jti, also when the claims are reused.
            var jti = oldAccessToken.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.JwtId);
            if (jti != null)
            {
                oldAccessToken.Claims.Remove(jti);
                oldAccessToken.Claims.Add(new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex)));
            }

            accessTokenString = await TokenService.CreateSecurityTokenAsync(oldAccessToken);
        }

        // A down-scoped access token must not shrink the grant stored with the refresh token.
        var handle = await RefreshTokenService.UpdateRefreshTokenAsync(request.ValidatedRequest.RefreshTokenHandle, request.ValidatedRequest.RefreshToken, request.ValidatedRequest.Client, isDownScoped ? null : newAccessToken);

        return new TokenResponse
        {
            IdentityToken = await CreateIdTokenFromRefreshTokenRequestAsync(request.ValidatedRequest, accessTokenString),
            AccessToken = accessTokenString,
            AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
            RefreshToken = handle,
            Custom = request.CustomResponse,
            Scope = (newAccessToken ?? oldAccessToken).Scopes.ToSpaceSeparatedString()
        };
    }

    /// <summary>
    /// Processes the response for device code grant request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    protected virtual async Task<TokenResponse> ProcessDeviceCodeRequestAsync(TokenRequestValidationResult request)
    {
        if (request.ValidatedRequest.DeviceCode?.AuthorizedScopes is null)
        {
            throw new InvalidOperationException("A device code request was validated without an authorized device code.");
        }

        ResponseAndHostingLog.CreatingDeviceCodeResponse(Logger);

        var (accessToken, refreshToken) = await CreateAccessTokenAsync(request.ValidatedRequest);
        var response = new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
            Custom = request.CustomResponse,
            Scope = request.ValidatedRequest.DeviceCode.AuthorizedScopes.ToSpaceSeparatedString()
        };

        if (refreshToken.IsPresent())
        {
            response.RefreshToken = refreshToken;
        }

        if (request.ValidatedRequest.DeviceCode.IsOpenId)
        {
            Client? client = null;
            if (request.ValidatedRequest.DeviceCode.ClientId != null)
            {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.DeviceCode.ClientId);
            }
            if (client == null)
            {
                throw new InvalidOperationException("Client does not exist anymore.");
            }

            var parsedScopesResult = ScopeParser.ParseScopeValues(request.ValidatedRequest.DeviceCode.AuthorizedScopes);
            var validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);

            var tokenRequest = new TokenCreationRequest
            {
                Subject = request.ValidatedRequest.DeviceCode.Subject,
                ValidatedResources = validatedResources,
                AccessTokenToHash = response.AccessToken,
                ValidatedRequest = request.ValidatedRequest
            };

            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
            var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            response.IdentityToken = jwt;
        }

        return response;
    }

    /// <summary>
    /// Creates the response for an extension grant request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    protected virtual Task<TokenResponse> ProcessExtensionGrantRequestAsync(TokenRequestValidationResult request)
    {
        ResponseAndHostingLog.CreatingExtensionGrantResponse(Logger);

        return ProcessTokenRequestAsync(request);
    }

    /// <summary>
    /// Creates the response for a token request.
    /// </summary>
    /// <param name="validationResult">The validation result.</param>
    /// <returns></returns>
    protected virtual async Task<TokenResponse> ProcessTokenRequestAsync(TokenRequestValidationResult validationResult)
    {
        (var accessToken, var refreshToken) = await CreateAccessTokenAsync(validationResult.ValidatedRequest);
        var response = new TokenResponse
        {
            AccessToken = accessToken,
            AccessTokenLifetime = validationResult.ValidatedRequest.AccessTokenLifetime,
            Custom = validationResult.CustomResponse,
            Scope = validationResult.ValidatedRequest.ValidatedResources.RawScopeValues.ToSpaceSeparatedString()
        };

        if (refreshToken.IsPresent())
        {
            response.RefreshToken = refreshToken;
        }

        return response;
    }

    /// <summary>
    /// Creates the access/refresh token.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    /// <exception cref="System.InvalidOperationException">Client does not exist anymore.</exception>
    protected virtual async Task<(string accessToken, string? refreshToken)> CreateAccessTokenAsync(ValidatedTokenRequest request)
    {
        TokenCreationRequest tokenRequest;
        bool createRefreshToken;

        if (request.AuthorizationCode != null)
        {
            createRefreshToken = request.AuthorizationCode.RequestedScopes.Contains(OidcServerConstants.StandardScopes.OfflineAccess);

            Client? client = null;
            if (request.AuthorizationCode.ClientId != null)
            {
                client = await Clients.FindEnabledClientByIdAsync(request.AuthorizationCode.ClientId);
            }
            if (client == null)
            {
                throw new InvalidOperationException("Client does not exist anymore.");
            }

            var parsedScopesResult = ScopeParser.ParseScopeValues(request.AuthorizationCode.RequestedScopes);
            var validatedResources = ResourceIndicators.Apply(
                await Resources.CreateResourceValidationResult(parsedScopesResult),
                request.RequestedResourceIndicators);

            tokenRequest = new TokenCreationRequest
            {
                Subject = request.AuthorizationCode.Subject,
                Description = request.AuthorizationCode.Description,
                ValidatedResources = validatedResources,
                ValidatedRequest = request
            };
        }
        else if (request.DeviceCode is { AuthorizedScopes: not null })
        {
            createRefreshToken = request.DeviceCode.AuthorizedScopes!.Contains(OidcServerConstants.StandardScopes.OfflineAccess);

            Client? client = null;
            if (request.DeviceCode.ClientId != null)
            {
                client = await Clients.FindEnabledClientByIdAsync(request.DeviceCode.ClientId);
            }
            if (client == null)
            {
                throw new InvalidOperationException("Client does not exist anymore.");
            }

            var parsedScopesResult = ScopeParser.ParseScopeValues(request.DeviceCode.AuthorizedScopes);
            var validatedResources = ResourceIndicators.Apply(
                await Resources.CreateResourceValidationResult(parsedScopesResult),
                requested: null);

            tokenRequest = new TokenCreationRequest
            {
                Subject = request.DeviceCode.Subject,
                Description = request.DeviceCode.Description,
                ValidatedResources = validatedResources,
                ValidatedRequest = request
            };
        }
        else
        {
            createRefreshToken = request.ValidatedResources.Resources.OfflineAccess;

            tokenRequest = new TokenCreationRequest
            {
                Subject = request.Subject,
                ValidatedResources = request.ValidatedResources,
                ValidatedRequest = request
            };
        }

        var at = await TokenService.CreateAccessTokenAsync(tokenRequest);
        var accessToken = await TokenService.CreateSecurityTokenAsync(at);

        if (createRefreshToken)
        {
            // The refresh token remembers every resource the grant was authorized for, not only the one selected now.
            var authorizedResourceIndicators = request.AuthorizationCode != null
                ? request.AuthorizationCode.RequestedResourceIndicators
                : request.RequestedResourceIndicators;
            var refreshToken = await RefreshTokenService.CreateRefreshTokenAsync(tokenRequest.Subject!, at, request.Client, authorizedResourceIndicators);
            return (accessToken, refreshToken);
        }

        return (accessToken, null);
    }

    /// <summary>
    /// Creates an id_token for a refresh token request if identity resources have been requested.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="newAccessToken">The new access token.</param>
    /// <returns></returns>
    protected virtual async Task<string?> CreateIdTokenFromRefreshTokenRequestAsync(ValidatedTokenRequest request, string newAccessToken)
    {
        if (request.RefreshToken is null)
        {
            throw new InvalidOperationException("A refresh token request was validated without a refresh token.");
        }

        if (request.RefreshToken.Scopes.Contains(OidcConstants.StandardScopes.OpenId))
        {
            var oldAccessToken = request.RefreshToken.AccessToken;

            var parsedScopesResult = ScopeParser.ParseScopeValues(oldAccessToken.Scopes);
            var validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);

            var tokenRequest = new TokenCreationRequest
            {
                Subject = request.RefreshToken.Subject,
                ValidatedResources = validatedResources,
                ValidatedRequest = request,
                AccessTokenToHash = newAccessToken
            };

            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
            return await TokenService.CreateSecurityTokenAsync(idToken);
        }

        return null;
    }
}
