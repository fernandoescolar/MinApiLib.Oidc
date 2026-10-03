namespace MinApiLib.Oidc.Server.Features.LocalApi;

/// <summary>
/// Authentication handler for validating access token from the local OidcServer
/// </summary>
public class LocalApiAuthenticationHandler : AuthenticationHandler<LocalApiAuthenticationOptions>
{
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger _logger;

    /// <inheritdoc />
    public LocalApiAuthenticationHandler(IOptionsMonitor<LocalApiAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ITokenValidator tokenValidator)
        : base(options, logger, encoder)
    {
        _tokenValidator = tokenValidator;
        _logger = logger.CreateLogger<LocalApiAuthenticationHandler>();
    }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new LocalApiAuthenticationEvents Events
    {
        get => (LocalApiAuthenticationEvents)base.Events!;
        set => base.Events = value;
    }

    /// <inheritdoc/>
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new LocalApiAuthenticationEvents());

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ResponseAndHostingLog.HandleAuthenticateCalled(_logger);

        string? token = null;

        string? authorization = Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        var isDPoPScheme = false;
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorization.Substring("Bearer ".Length).Trim();
        }
        else if (authorization.StartsWith(OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP + " ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorization.Substring(OidcConstants.AuthenticationSchemes.AuthorizationHeaderDPoP.Length + 1).Trim();
            isDPoPScheme = true;
        }

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("No Access Token is sent.");
        }

        ResponseAndHostingLog.TokenFound(_logger, token);

        TokenValidationResult result = await _tokenValidator.ValidateAccessTokenAsync(token, Options.ExpectedScope);

        if (result.IsError)
        {
            ResponseAndHostingLog.TokenValidationFailed(_logger);

            return AuthenticateResult.Fail(result.Error);
        }

        // RFC 9449 section 7 and CVE-2024-49755: a DPoP-bound token needs the DPoP scheme and a proof of the bound key.
        var dpopError = await DPoPRequest.ValidateProtectedResourceRequestAsync(Context, token, isDPoPScheme, result.Claims);
        if (dpopError != null)
        {
            ResponseAndHostingLog.LocalApiRequestRejected(_logger, dpopError);
            return AuthenticateResult.Fail(dpopError);
        }

        ResponseAndHostingLog.TokenValidated(_logger);

        ClaimsIdentity claimsIdentity = new ClaimsIdentity(result.Claims, Scheme.Name, JwtClaimTypes.Name, JwtClaimTypes.Role);
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        AuthenticationProperties authenticationProperties = new AuthenticationProperties();

        if (Options.SaveToken)
        {
            authenticationProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = token }
            });
        }

        var claimsTransformationContext = new ClaimsTransformationContext
        {
            Principal = claimsPrincipal,
            HttpContext = Context
        };

        await Events.ClaimsTransformation(claimsTransformationContext);

        AuthenticationTicket authenticationTicket = new AuthenticationTicket(claimsTransformationContext.Principal, authenticationProperties, Scheme.Name);
        return AuthenticateResult.Success(authenticationTicket);
    }
}
