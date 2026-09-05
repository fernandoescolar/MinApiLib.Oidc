namespace MinApiLib.OidcServer.Services;

/// <summary>
/// Default token service
/// </summary>
[RegisterInIServiceCollection(typeof(ITokenService))]
public class DefaultTokenService : ITokenService
{
    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// The HTTP context accessor
    /// </summary>
    protected readonly IHttpContextAccessor ContextAccessor;

    /// <summary>
    /// The issuer name service.
    /// </summary>
    protected readonly IIssuerNameService IssuerNameService;

    /// <summary>
    /// The claims provider
    /// </summary>
    protected readonly IClaimsService ClaimsProvider;

    /// <summary>
    /// The reference token store
    /// </summary>
    protected readonly IReferenceTokenStore ReferenceTokenStore;

    /// <summary>
    /// The signing service
    /// </summary>
    protected readonly ITokenCreationService CreationService;

    /// <summary>
    /// The clock
    /// </summary>
    protected readonly TimeProvider Clock;

    /// <summary>
    /// The key material service
    /// </summary>
    protected readonly IKeyMaterialService KeyMaterialService;

    /// <summary>
    /// The OidcServer options
    /// </summary>
    protected readonly OidcServerOptions Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTokenService" /> class.
    /// </summary>
    /// <param name="claimsProvider">The claims provider.</param>
    /// <param name="referenceTokenStore">The reference token store.</param>
    /// <param name="creationService">The signing service.</param>
    /// <param name="issuerNameService">The issuer name service.</param>
    /// <param name="contextAccessor">The HTTP context accessor.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="keyMaterialService"></param>
    /// <param name="options">The OidcServer options</param>
    /// <param name="logger">The logger.</param>
    public DefaultTokenService(
        IClaimsService claimsProvider,
        IReferenceTokenStore referenceTokenStore,
        ITokenCreationService creationService,
        IHttpContextAccessor contextAccessor,
        IIssuerNameService issuerNameService,
        TimeProvider clock,
        IKeyMaterialService keyMaterialService,
        OidcServerOptions options,
        ILogger<DefaultTokenService> logger)
    {
        ContextAccessor = contextAccessor;
        IssuerNameService = issuerNameService;
        ClaimsProvider = claimsProvider;
        ReferenceTokenStore = referenceTokenStore;
        CreationService = creationService;
        Clock = clock;
        KeyMaterialService = keyMaterialService;
        Options = options;
        Logger = logger;
    }

    /// <summary>
    /// Creates an identity token.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <returns>
    /// An identity token
    /// </returns>
    public virtual async Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request)
    {
        ServicesLog.CreatingIdentityToken(Logger);
        request.Validate();

        var credential = await KeyMaterialService.GetSigningCredentialsAsync(request.ValidatedRequest.Client.AllowedIdentityTokenSigningAlgorithms);
        if (credential == null)
        {
            throw new InvalidOperationException("No signing credential is configured.");
        }

        var signingAlgorithm = credential.Algorithm;

        var claims = new List<Claim>();

        if (request.Nonce.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.Nonce, request.Nonce));
        }

        claims.Add(new Claim(JwtClaimTypes.IssuedAt, Clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));

        if (request.AccessTokenToHash.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.AccessTokenHash, CryptoHelper.CreateHashClaimValue(request.AccessTokenToHash, signingAlgorithm)));
        }

        if (request.AuthorizationCodeToHash.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.AuthorizationCodeHash, CryptoHelper.CreateHashClaimValue(request.AuthorizationCodeToHash, signingAlgorithm)));
        }

        if (request.StateHash.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.StateHash, request.StateHash));
        }

        if (request.ValidatedRequest.SessionId.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.SessionId, request.ValidatedRequest.SessionId));
        }

        claims.AddRange(await ClaimsProvider.GetIdentityTokenClaimsAsync(
            request.Subject ?? throw new InvalidOperationException("An identity token requires a subject."),
            request.ValidatedResources,
            request.IncludeAllIdentityClaims,
            request.ValidatedRequest));

        var issuer = await IssuerNameService.GetCurrentAsync();

        var token = new Token(OidcConstants.TokenTypes.IdentityToken)
        {
            CreationTime = Clock.GetUtcNow().UtcDateTime,
            Audiences = { request.ValidatedRequest.Client.ClientId },
            Issuer = issuer,
            Lifetime = request.ValidatedRequest.Client.IdentityTokenLifetime,
            Claims = claims.Distinct(new ClaimComparer()).ToList(),
            ClientId = request.ValidatedRequest.Client.ClientId,
            AccessTokenType = request.ValidatedRequest.AccessTokenType,
            AllowedSigningAlgorithms = request.ValidatedRequest.Client.AllowedIdentityTokenSigningAlgorithms
        };

        return token;
    }

    /// <summary>
    /// Creates an access token.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <returns>
    /// An access token
    /// </returns>
    public virtual async Task<Token> CreateAccessTokenAsync(TokenCreationRequest request)
    {
        ServicesLog.CreatingAccessToken(Logger);
        request.Validate();

        var claims = new List<Claim>();
        claims.AddRange(await ClaimsProvider.GetAccessTokenClaimsAsync(
            request.Subject,
            request.ValidatedResources,
            request.ValidatedRequest));

        if (request.ValidatedRequest.Client.IncludeJwtId)
        {
            claims.Add(new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex)));
        }

        if (request.ValidatedRequest.SessionId.IsPresent())
        {
            claims.Add(new Claim(JwtClaimTypes.SessionId, request.ValidatedRequest.SessionId));
        }

        claims.Add(new Claim(JwtClaimTypes.IssuedAt, Clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ClaimValueTypes.Integer64));

        var issuer = await IssuerNameService.GetCurrentAsync();
        var token = new Token(OidcConstants.TokenTypes.AccessToken)
        {
            CreationTime = Clock.GetUtcNow().UtcDateTime,
            Issuer = issuer,
            Lifetime = request.ValidatedRequest.AccessTokenLifetime,
            Claims = claims.Distinct(new ClaimComparer()).ToList(),
            ClientId = request.ValidatedRequest.Client.ClientId,
            Description = request.Description,
            AccessTokenType = request.ValidatedRequest.AccessTokenType,
            AllowedSigningAlgorithms = request.ValidatedResources.Resources.ApiResources.FindMatchingSigningAlgorithms()
        };

        foreach (var aud in request.ValidatedResources.Resources.ApiResources.Select(x => x.Name).Distinct())
        {
            token.Audiences.Add(aud);
        }

        // RFC 9068 section 2.2 requires aud; tokens for API scopes without an API resource get the static audience.
        if (Options.EmitStaticAudienceClaim || token.Audiences.Count == 0)
        {
            token.Audiences.Add(string.Format(CultureInfo.InvariantCulture, Constants.AccessTokenAudienceFormat, issuer.EnsureTrailingSlash()));
        }

        if (request.ValidatedRequest.Confirmation.IsPresent())
        {
            token.Confirmation = request.ValidatedRequest.Confirmation;
        }
        else
        {
            if (Options.MutualTls.AlwaysEmitConfirmationClaim)
            {
                // tokens are created while handling a request
                var clientCertificate = await ContextAccessor.HttpContext!.Connection.GetClientCertificateAsync();
                if (clientCertificate != null)
                {
                    token.Confirmation = clientCertificate.CreateThumbprintCnf();
                }
            }
        }

        return token;
    }

    /// <summary>
    /// Creates a serialized and protected security token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>
    /// A security token in serialized form
    /// </returns>
    /// <exception cref="System.InvalidOperationException">Invalid token type.</exception>
    public virtual async Task<string> CreateSecurityTokenAsync(Token token)
    {
        string tokenResult;

        if (token.Type == OidcConstants.TokenTypes.AccessToken)
        {
            if (token.AccessTokenType == AccessTokenType.Jwt)
            {
                ServicesLog.CreatingJwtAccessToken(Logger);

                tokenResult = await CreationService.CreateTokenAsync(token);
            }
            else
            {
                ServicesLog.CreatingReferenceAccessToken(Logger);

                var handle = await ReferenceTokenStore.StoreReferenceTokenAsync(token);

                tokenResult = handle;
            }
        }
        else if (token.Type == OidcConstants.TokenTypes.IdentityToken)
        {
            ServicesLog.CreatingJwtIdentityToken(Logger);

            tokenResult = await CreationService.CreateTokenAsync(token);
        }
        else
        {
            throw new InvalidOperationException("Invalid token type.");
        }

        return tokenResult;
    }
}
