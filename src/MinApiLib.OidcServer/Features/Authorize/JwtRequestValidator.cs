namespace MinApiLib.OidcServer.Features.Authorize;

/// <summary>
/// Validates JWT authorization request objects
/// </summary>
[RegisterInIServiceCollection]
public class JwtRequestValidator
{
    private readonly string? _audienceUri;
    private readonly IIssuerNameService _issuerNameService;
    private string _resolvedAudienceUri = default!;

    /// <summary>
    /// JWT handler
    /// </summary>
    protected JwtSecurityTokenHandler Handler = new JwtSecurityTokenHandler
    {
        MapInboundClaims = false
    };

    /// <summary>
    /// The audience URI to use
    /// </summary>
    protected string AudienceUri
    {
        get
        {
            if (_audienceUri.IsPresent())
            {
                return _audienceUri;
            }

            // Resolved at the start of ValidateAsync (the validator is transient).
            return _resolvedAudienceUri;
        }
    }

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The optione
    /// </summary>
    protected readonly OidcServerOptions Options;

    /// <summary>
    /// Instantiates an instance of private_key_jwt secret validator
    /// </summary>
    public JwtRequestValidator(IIssuerNameService issuerNameService, OidcServerOptions options, ILogger<JwtRequestValidator> logger)
    {
        _issuerNameService = issuerNameService;

        Options = options;
        Logger = logger;
    }

    /// <summary>
    /// Instantiates an instance of private_key_jwt secret validator (used for testing)
    /// </summary>
    internal JwtRequestValidator(string audience, ILogger<JwtRequestValidator> logger)
    {
        _audienceUri = audience;
        Logger = logger;

        // Test-only constructor: the audience is fixed, so the issuer name service is never used; options are not provided.
        _issuerNameService = null!;
        Options = null!;
    }

    /// <summary>
    /// Validates a JWT request object
    /// </summary>
    /// <param name="client">The client</param>
    /// <param name="jwtTokenString">The JWT</param>
    /// <returns></returns>
    public virtual async Task<JwtRequestValidationResult> ValidateAsync(Client client, string jwtTokenString)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (String.IsNullOrWhiteSpace(jwtTokenString)) throw new ArgumentNullException(nameof(jwtTokenString));

        if (_audienceUri.IsMissing())
        {
            _resolvedAudienceUri = await _issuerNameService.GetCurrentAsync();
        }

        var fail = new JwtRequestValidationResult { IsError = true };

        List<SecurityKey> trustedKeys;
        try
        {
            trustedKeys = await GetKeysAsync(client);
        }
        catch (Exception e)
        {
            RequestValidationLog.CouldNotParseClientSecrets(Logger, e);
            return fail;
        }

        if (trustedKeys.Count == 0)
        {
            RequestValidationLog.NoKeysToValidateJwt(Logger);
            return fail;
        }

        JwtSecurityToken jwtSecurityToken;
        try
        {
            jwtSecurityToken = await ValidateJwtAsync(jwtTokenString, trustedKeys, client);
        }
        catch (Exception e)
        {
            RequestValidationLog.JwtRequestValidationError(Logger, e);
            return fail;
        }

        if (jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.Request) ||
            jwtSecurityToken.Payload.ContainsKey(OidcConstants.AuthorizeRequest.RequestUri))
        {
            RequestValidationLog.JwtPayloadContainsRequest(Logger);
            return fail;
        }

        var payload = await ProcessPayloadAsync(jwtSecurityToken);

        var result = new JwtRequestValidationResult
        {
            IsError = false,
            Payload = payload
        };

        RequestValidationLog.JwtRequestValidationSuccess(Logger);
        return result;
    }

    /// <summary>
    /// Retrieves keys for a given client
    /// </summary>
    /// <param name="client">The client</param>
    /// <returns></returns>
    protected virtual Task<List<SecurityKey>> GetKeysAsync(Client client)
    {
        return client.ClientSecrets.GetKeysAsync();
    }

    /// <summary>
    /// Validates the JWT token
    /// </summary>
    /// <param name="jwtTokenString">JWT as a string</param>
    /// <param name="keys">The keys</param>
    /// <param name="client">The client</param>
    /// <returns></returns>
    protected virtual Task<JwtSecurityToken> ValidateJwtAsync(string jwtTokenString, IEnumerable<SecurityKey> keys, Client client)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,

            ValidIssuer = client.ClientId,
            ValidateIssuer = true,

            ValidAudience = AudienceUri,
            ValidateAudience = true,

            RequireSignedTokens = true,
            RequireExpirationTime = true
        };

        if (Options.StrictJarValidation)
        {
            tokenValidationParameters.ValidTypes = new[] { JwtClaimTypes.JwtTypes.AuthorizationRequest };
        }

        Handler.ValidateToken(jwtTokenString, tokenValidationParameters, out var token);

        return Task.FromResult((JwtSecurityToken)token);
    }

    /// <summary>
    /// Processes the JWT contents
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns></returns>
    protected virtual Task<Dictionary<string, string>> ProcessPayloadAsync(JwtSecurityToken token)
    {
        var payload = new Dictionary<string, string>();
        foreach (var key in token.Payload.Keys)
        {
            if (!Constants.Filters.JwtRequestClaimTypesFilter.Contains(key))
            {
                var value = token.Payload[key];

                switch (value)
                {
                    case string s:
                        payload.Add(key, s);
                        break;
                    case JsonObject jobj:
                        payload.Add(key, jobj.ToString());
                        break;
                    case JsonArray jarr:
                        payload.Add(key, jarr.ToString());
                        break;
                }
            }
        }

        return Task.FromResult(payload);
    }
}
