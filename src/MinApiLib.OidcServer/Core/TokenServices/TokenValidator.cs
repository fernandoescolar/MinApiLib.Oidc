namespace MinApiLib.OidcServer.Core.TokenServices;

[RegisterInIServiceCollection(typeof(ITokenValidator))]
internal class TokenValidator : ITokenValidator
{
    private readonly ILogger _logger;
    private readonly OidcServerOptions _options;
    private readonly IIssuerNameService _issuerNameService;
    private readonly IReferenceTokenStore _referenceTokenStore;
    private readonly ICustomTokenValidator _customValidator;
    private readonly IClientStore _clients;
    private readonly IProfileService _profile;
    private readonly IKeyMaterialService _keys;
    private readonly TimeProvider _clock;
    private readonly TokenValidationLog _log;

    public TokenValidator(
        OidcServerOptions options,
        IIssuerNameService issuerNameService,
        IClientStore clients,
        IProfileService profile,
        IReferenceTokenStore referenceTokenStore,
        ICustomTokenValidator customValidator,
        IKeyMaterialService keys,
        TimeProvider clock,
        ILogger<TokenValidator> logger)
    {
        _options = options;
        _issuerNameService = issuerNameService;
        _clients = clients;
        _profile = profile;
        _referenceTokenStore = referenceTokenStore;
        _customValidator = customValidator;
        _keys = keys;
        _clock = clock;
        _logger = logger;

        _log = new TokenValidationLog();
    }

    public async Task<TokenValidationResult> ValidateIdentityTokenAsync(string token, string? clientId = null, bool validateLifetime = true)
    {
        _logger.StartIdentityTokenValidation();

        if (token.Length > _options.InputLengthRestrictions.Jwt)
        {
            _logger.JwtTooLong();
            return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        if (clientId.IsMissing())
        {
            clientId = GetClientIdFromJwt(token);

            if (clientId.IsMissing())
            {
                _logger.NoClientIdSupplied();
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }
        }

        _log.ClientId = clientId;
        _log.ValidateLifetime = validateLifetime;

        var client = await _clients.FindEnabledClientByIdAsync(clientId);
        if (client == null)
        {
            RequestValidationLog.UnknownOrDisabledClient(_logger, clientId);
            return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        _log.ClientName = client.ClientName;
        RequestValidationLog.ClientFound(_logger, client.ClientId, client.ClientName);

        var keys = await _keys.GetValidationKeysAsync();
        var result = await ValidateJwtAsync(token, keys, audience: clientId, validateLifetime: validateLifetime);

        result.Client = client;

        if (result.IsError)
        {
            LogError("Error validating JWT");
            return result;
        }

        RequestValidationLog.CallingCustomTokenValidator(_logger, _customValidator.GetType().FullName!);
        var customResult = await _customValidator.ValidateIdentityTokenAsync(result);

        if (customResult.IsError)
        {
            LogError("Custom validator failed: " + (customResult.Error ?? "unknown"));
            return customResult;
        }

        _log.Claims = customResult.Claims.ToClaimsDictionary();

        LogSuccess();
        return customResult;
    }

    public async Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string? expectedScope = null)
    {
        RequestValidationLog.StartAccessTokenValidation(_logger);

        _log.ExpectedScope = expectedScope;
        _log.ValidateLifetime = true;

        TokenValidationResult result;

        if (token.Contains('.'))
        {
            if (token.Length > _options.InputLengthRestrictions.Jwt)
            {
                RequestValidationLog.JwtTooLong(_logger);

                return new TokenValidationResult
                {
                    IsError = true,
                    Error = OidcConstants.ProtectedResourceErrors.InvalidToken,
                    ErrorDescription = "Token too long"
                };
            }

            _log.AccessTokenType = AccessTokenType.Jwt.ToString();
            result = await ValidateJwtAsync(
                token,
                await _keys.GetValidationKeysAsync());
        }
        else
        {
            if (token.Length > _options.InputLengthRestrictions.TokenHandle)
            {
                RequestValidationLog.TokenHandleTooLong(_logger);

                return new TokenValidationResult
                {
                    IsError = true,
                    Error = OidcConstants.ProtectedResourceErrors.InvalidToken,
                    ErrorDescription = "Token too long"
                };
            }

            _log.AccessTokenType = AccessTokenType.Reference.ToString();
            result = await ValidateReferenceAccessTokenAsync(token);
        }

        _log.Claims = result.Claims.ToClaimsDictionary();

        if (result.IsError)
        {
            return result;
        }

        var clientClaim = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.ClientId);
        if (clientClaim != null)
        {
            var client = await _clients.FindEnabledClientByIdAsync(clientClaim.Value);
            if (client == null)
            {
                RequestValidationLog.ClientDeletedOrDisabled(_logger, clientClaim.Value);

                result.IsError = true;
                result.Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                result.Claims = null!;

                return result;
            }
        }

        var subClaim = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
        if (subClaim != null)
        {
            var principal = Principal.Create("tokenvalidator", result.Claims.ToArray());

            if (result.ReferenceTokenId.IsPresent())
            {
                principal.Identities.First().AddClaim(new Claim(JwtClaimTypes.ReferenceTokenId, result.ReferenceTokenId));
            }

            var isActiveCtx = new IsActiveContext(principal, result.Client!, OidcServerConstants.ProfileIsActiveCallers.AccessTokenValidation);
            await _profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                RequestValidationLog.UserMarkedNotActive(_logger, subClaim.Value);

                result.IsError = true;
                result.Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                result.Claims = null!;

                return result;
            }
        }

        if (expectedScope.IsPresent())
        {
            var scope = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Scope && c.Value == expectedScope);
            if (scope == null)
            {
                LogError($"Checking for expected scope {expectedScope} failed");
                return Invalid(OidcConstants.ProtectedResourceErrors.InsufficientScope);
            }
        }

        RequestValidationLog.CallingCustomTokenValidator(_logger, _customValidator.GetType().FullName!);
        var customResult = await _customValidator.ValidateAccessTokenAsync(result);

        if (customResult.IsError)
        {
            LogError("Custom validator failed: " + (customResult.Error ?? "unknown"));
            return customResult;
        }

        _log.Claims = customResult.Claims.ToClaimsDictionary();

        LogSuccess();
        return customResult;
    }

    private async Task<TokenValidationResult> ValidateJwtAsync(string jwt, IEnumerable<SecurityKeyInfo> validationKeys, bool validateLifetime = true, string? audience = null)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = _options.TokenValidation.ValidateIssuer,
            ValidIssuer = await _issuerNameService.GetCurrentAsync(),
            ValidIssuers = _options.TokenValidation.ValidIssuers,
            IssuerSigningKeys = validationKeys.Select(k => k.Key),
            ValidateLifetime = validateLifetime
        };

        if (audience.IsPresent())
        {
            parameters.ValidAudience = audience;
        }
        else
        {
            parameters.ValidateAudience = false;
        }

        try
        {
            var id = handler.ValidateToken(jwt, parameters, out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (audience.IsMissing())
            {
                if (_options.AccessTokenJwtType.IsPresent())
                {
                    var type = jwtSecurityToken!.Header.Typ;
                    if (!string.Equals(type, _options.AccessTokenJwtType, StringComparison.Ordinal))
                    {
                        return new TokenValidationResult
                        {
                            IsError = true,
                            Error = "invalid JWT token type"
                        };
                    }

                }
            }

            var jwtId = id.FindFirst(JwtClaimTypes.JwtId);
            if (jwtId != null)
            {
                _log.JwtId = jwtId.Value;
            }

            Client? client = null;
            var clientId = id.FindFirst(JwtClaimTypes.ClientId);
            if (clientId != null)
            {
                client = await _clients.FindEnabledClientByIdAsync(clientId.Value);
                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }
            }

            var claims = id.Claims.ToList();

            var scopes = claims.Where(c => c.Type == JwtClaimTypes.Scope).ToArray();
            if (scopes.Length > 0)
            {
                foreach (var scope in scopes)
                {
                    if (scope.Value.Contains(' '))
                    {
                        claims.Remove(scope);

                        var values = scope.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var value in values)
                        {
                            claims.Add(new Claim(JwtClaimTypes.Scope, value));
                        }
                    }
                }
            }

            return new TokenValidationResult
            {
                IsError = false,

                Claims = claims,
                Client = client,
                Jwt = jwt
            };
        }
        catch (SecurityTokenExpiredException expiredException)
        {
            RequestValidationLog.JwtTokenExpired(_logger, expiredException, expiredException.Message);
            return Invalid(OidcConstants.ProtectedResourceErrors.ExpiredToken);
        }
        catch (Exception ex)
        {
            RequestValidationLog.JwtTokenValidationFailed(_logger, ex, ex.Message);
            return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }
    }

    private async Task<TokenValidationResult> ValidateReferenceAccessTokenAsync(string tokenHandle)
    {
        _log.TokenHandle = tokenHandle;
        var token = await _referenceTokenStore.GetReferenceTokenAsync(tokenHandle);

        if (token == null)
        {
            LogError("Invalid reference token.");
            return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        if (token.CreationTime.HasExceeded(token.Lifetime, _clock.GetUtcNow().UtcDateTime))
        {
            LogError("Token expired.");

            await _referenceTokenStore.RemoveReferenceTokenAsync(tokenHandle);
            return Invalid(OidcConstants.ProtectedResourceErrors.ExpiredToken);
        }

        Client? client = null;
        if (token.ClientId != null)
        {
            client = await _clients.FindEnabledClientByIdAsync(token.ClientId);
        }

        if (client == null)
        {
            LogError($"Client deleted or disabled: {token.ClientId}");
            return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
        }

        return new TokenValidationResult
        {
            IsError = false,

            Client = client,
            Claims = ReferenceTokenToClaims(token),
            ReferenceToken = token,
            ReferenceTokenId = tokenHandle
        };
    }

    private static List<Claim> ReferenceTokenToClaims(Token token)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Issuer, token.Issuer),
            new Claim(JwtClaimTypes.NotBefore, new DateTimeOffset(token.CreationTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtClaimTypes.Expiration, new DateTimeOffset(token.CreationTime).AddSeconds(token.Lifetime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        foreach (var aud in token.Audiences)
        {
            claims.Add(new Claim(JwtClaimTypes.Audience, aud));
        }

        if (token.Confirmation.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.Confirmation, token.Confirmation, OidcServerConstants.ClaimValueTypes.Json));
        }

        claims.AddRange(token.Claims);
        return claims;
    }

    private string? GetClientIdFromJwt(string token)
    {
        try
        {
            var jwt = new JwtSecurityToken(token);
            var clientId = jwt.Audiences.FirstOrDefault();

            return clientId;
        }
        catch (Exception ex)
        {
            RequestValidationLog.MalformedJwt(_logger, ex, ex.Message);
            return null;
        }
    }

    private TokenValidationResult Invalid(string error)
    {
        return new TokenValidationResult
        {
            IsError = true,
            Error = error
        };
    }

    private void LogError(string message)
    {
        RequestValidationLog.TokenValidationError(_logger, message, _log);
    }

    private void LogSuccess()
    {
        RequestValidationLog.TokenValidationSuccess(_logger, _log);
    }
}

internal static partial class TokenValidatorLoggerExtensions
{
    [LoggerMessage(0x5501, LogLevel.Trace, "Start identity token validation")]
    public static partial void StartIdentityTokenValidation(this ILogger logger);

    [LoggerMessage(0x5502, LogLevel.Trace, "JWT too long")]
    public static partial void JwtTooLong(this ILogger logger);

    [LoggerMessage(0x5503, LogLevel.Trace, "No clientId supplied, can't find id in identity token.")]
    public static partial void NoClientIdSupplied(this ILogger logger);




}
