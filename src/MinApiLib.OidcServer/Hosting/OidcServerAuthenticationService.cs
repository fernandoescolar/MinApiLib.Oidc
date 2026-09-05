namespace MinApiLib.OidcServer.Hosting;

internal class OidcServerAuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationService _inner;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly TimeProvider _clock;
    private readonly IUserSession _session;
    private readonly ILogger<OidcServerAuthenticationService> _logger;

    public OidcServerAuthenticationService(
        [FromKeyedServices(DecoratorServiceCollectionExtensions.InnerServiceKey)] IAuthenticationService inner,
        IAuthenticationSchemeProvider schemes,
        TimeProvider clock,
        IUserSession session,
        ILogger<OidcServerAuthenticationService> logger)
    {
        _inner = inner;

        _schemes = schemes;
        _clock = clock;
        _session = session;
        _logger = logger;
    }

    public async Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        var defaultScheme = await _schemes.GetDefaultSignInSchemeAsync();
        var cookieScheme = await context.GetCookieAuthenticationSchemeAsync();

        if ((scheme == null && defaultScheme?.Name == cookieScheme) || scheme == cookieScheme)
        {
            AugmentPrincipal(principal);

            properties ??= new AuthenticationProperties();
            await _session.CreateSessionIdAsync(principal, properties);
        }

        await _inner.SignInAsync(context, scheme, principal, properties);
    }

    private void AugmentPrincipal(ClaimsPrincipal principal)
    {
        ResponseAndHostingLog.AugmentingSignInContext(_logger);

        AssertRequiredClaims(principal);
        AugmentMissingClaims(principal, _clock.GetUtcNow().UtcDateTime);
    }

    public async Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        var defaultScheme = await _schemes.GetDefaultSignOutSchemeAsync();
        var cookieScheme = await context.GetCookieAuthenticationSchemeAsync();

        if ((scheme == null && defaultScheme?.Name == cookieScheme) || scheme == cookieScheme)
        {
            context.SetSignOutCalled();
        }

        await _inner.SignOutAsync(context, scheme, properties);
    }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        return _inner.AuthenticateAsync(context, scheme);
    }

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        return _inner.ChallengeAsync(context, scheme, properties);
    }

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        return _inner.ForbidAsync(context, scheme, properties);
    }

    private void AssertRequiredClaims(ClaimsPrincipal principal)
    {
        if (principal.Identities.Count() != 1) throw new InvalidOperationException("only a single identity supported");
        if (principal.FindFirst(JwtClaimTypes.Subject) == null) throw new InvalidOperationException("sub claim is missing");
    }

    private void AugmentMissingClaims(ClaimsPrincipal principal, DateTime authTime)
    {
        var identity = principal.Identities.First();

        var amr = identity.FindFirst(ClaimTypes.AuthenticationMethod);
        if (amr != null &&
            identity.FindFirst(JwtClaimTypes.IdentityProvider) == null &&
            identity.FindFirst(JwtClaimTypes.AuthenticationMethod) == null)
        {
            ResponseAndHostingLog.RemovingAmrClaim(_logger, amr.Value);
            identity.RemoveClaim(amr);

            ResponseAndHostingLog.AddingIdpClaim(_logger, amr.Value);
            identity.AddClaim(new Claim(JwtClaimTypes.IdentityProvider, amr.Value));

            ResponseAndHostingLog.AddingAmrClaim(_logger, Constants.ExternalAuthenticationMethod);
            identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, Constants.ExternalAuthenticationMethod));
        }

        if (identity.FindFirst(JwtClaimTypes.IdentityProvider) == null)
        {
            ResponseAndHostingLog.AddingIdpClaim(_logger, OidcServerConstants.LocalIdentityProvider);
            identity.AddClaim(new Claim(JwtClaimTypes.IdentityProvider, OidcServerConstants.LocalIdentityProvider));
        }

        if (identity.FindFirst(JwtClaimTypes.AuthenticationMethod) == null)
        {
            if (identity.FindFirst(JwtClaimTypes.IdentityProvider)!.Value == OidcServerConstants.LocalIdentityProvider)
            {
                ResponseAndHostingLog.AddingAmrClaim(_logger, OidcConstants.AuthenticationMethods.Password);
                identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, OidcConstants.AuthenticationMethods.Password));
            }
            else
            {
                ResponseAndHostingLog.AddingAmrClaim(_logger, Constants.ExternalAuthenticationMethod);
                identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, Constants.ExternalAuthenticationMethod));
            }
        }

        if (identity.FindFirst(JwtClaimTypes.AuthenticationTime) == null)
        {
            var time = new DateTimeOffset(authTime).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

            ResponseAndHostingLog.AddingAuthTimeClaim(_logger, time);
            identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationTime, time, ClaimValueTypes.Integer64));
        }
    }
}
