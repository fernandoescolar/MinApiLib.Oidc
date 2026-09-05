namespace IdentityServer.Validation;

/// <summary>
/// Validates a secret based on RS256 signed JWT token
/// </summary>
public class PrivateKeyJwtSecretValidator : ISecretValidator
{
    private readonly IIssuerNameService _issuerNameService;
    private readonly IReplayCache _replayCache;
    private readonly ILogger _logger;

    private const string Purpose = nameof(PrivateKeyJwtSecretValidator);

    /// <summary>
    /// Instantiates an instance of private_key_jwt secret validator
    /// </summary>
    public PrivateKeyJwtSecretValidator(IIssuerNameService issuerNameService, IReplayCache replayCache, ILogger<PrivateKeyJwtSecretValidator> logger)
    {
        _issuerNameService = issuerNameService;
        _replayCache = replayCache;
        _logger = logger;
    }

    /// <summary>
    /// Validates a secret
    /// </summary>
    /// <param name="secrets">The stored secrets.</param>
    /// <param name="parsedSecret">The received secret.</param>
    /// <returns>
    /// A validation result
    /// </returns>
    /// <exception cref="System.ArgumentException">ParsedSecret.Credential is not a JWT token</exception>
    public async Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        var fail = new SecretValidationResult { Success = false };
        var success = new SecretValidationResult { Success = true };

        if (parsedSecret.Type != IdentityServerConstants.ParsedSecretTypes.JwtBearer)
        {
            return fail;
        }

        if (!(parsedSecret.Credential is string jwtTokenString))
        {
            SecretLog.CredentialNotString(_logger);
            return fail;
        }

        List<SecurityKey> trustedKeys;
        try
        {
            trustedKeys = await secrets.GetKeysAsync();
        }
        catch (Exception e)
        {
            SecretLog.CouldNotParseSecrets(_logger, e);
            return fail;
        }

        if (trustedKeys.Count == 0)
        {
            SecretLog.NoKeysToValidateClientAssertion(_logger);
            return fail;
        }

        // RFC 7523 section 3 and RFC 9700 section 4.15: the audience is the token endpoint URL or the issuer identifier.
        var issuer = await _issuerNameService.GetCurrentAsync();
        var validAudiences = new[]
        {
            string.Concat(issuer.EnsureTrailingSlash(), Constants.ProtocolRoutePaths.Token),
            issuer
        };

        var tokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKeys = trustedKeys,
            ValidateIssuerSigningKey = true,

            ValidIssuer = parsedSecret.Id,
            ValidateIssuer = true,

            ValidAudiences = validAudiences,
            ValidateAudience = true,

            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidAlgorithms = IdentityServerConstants.SupportedSigningAlgorithms,

            ClockSkew = TimeSpan.FromMinutes(5)
        };
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(jwtTokenString, tokenValidationParameters, out var token);

            var jwtToken = (JwtSecurityToken)token;
            if (jwtToken.Subject != jwtToken.Issuer)
            {
                SecretLog.InvalidSubOrIss(_logger);
                return fail;
            }
            var exp = jwtToken.Payload.Expiration;
            if (!exp.HasValue)
            {
                SecretLog.ExpMissing(_logger);
                return fail;
            }

            var jti = jwtToken.Payload.Jti;
            if (jti.IsMissing())
            {
                SecretLog.JtiMissing(_logger);
                return fail;
            }

            if (await _replayCache.ExistsAsync(Purpose, jti))
            {
                SecretLog.JtiReplayDetected(_logger);
                return fail;
            }
            else
            {
                await _replayCache.AddAsync(Purpose, jti, DateTimeOffset.FromUnixTimeSeconds(exp.Value).AddMinutes(5));
            }

            return success;
        }
        catch (Exception e)
        {
            SecretLog.JwtTokenValidationError(_logger, e);
            return fail;
        }
    }
}
