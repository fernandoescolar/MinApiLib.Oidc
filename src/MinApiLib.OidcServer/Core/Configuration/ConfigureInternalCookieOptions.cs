namespace MinApiLib.OidcServer.Core.Configuration;

[RegisterInIServiceCollection(typeof(IConfigureOptions<CookieAuthenticationOptions>), ServiceLifetime.Singleton)]
internal class ConfigureInternalCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    private readonly OidcServerOptions _idsrv;

    public ConfigureInternalCookieOptions(OidcServerOptions idsrv)
    {
        _idsrv = idsrv;
    }

    public void Configure(CookieAuthenticationOptions options)
    {
    }

    public void Configure(string? name, CookieAuthenticationOptions options)
    {
        if (name == DefaultCookieAuthenticationScheme)
        {
            options.SlidingExpiration = _idsrv.Authentication.CookieSlidingExpiration;
            options.ExpireTimeSpan = _idsrv.Authentication.CookieLifetime;
            options.Cookie.Name = DefaultCookieAuthenticationScheme;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = _idsrv.Authentication.CookieSameSiteMode;

            options.LoginPath = ExtractLocalUrl(_idsrv.UserInteraction.LoginUrl);
            options.LogoutPath = ExtractLocalUrl(_idsrv.UserInteraction.LogoutUrl);
            if (_idsrv.UserInteraction.LoginReturnUrlParameter != null)
            {
                options.ReturnUrlParameter = _idsrv.UserInteraction.LoginReturnUrlParameter;
            }
        }

        if (name == ExternalCookieAuthenticationScheme)
        {
            options.Cookie.Name = ExternalCookieAuthenticationScheme;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = _idsrv.Authentication.CookieSameSiteMode;
        }
    }

    private static string? ExtractLocalUrl(string? url)
    {
        if (url.IsLocalUrl())
        {
            if (url.StartsWith("~/", StringComparison.Ordinal))
            {
                url = url[1..];
            }

            return url;
        }

        return null;
    }
}

[RegisterInIServiceCollection(typeof(IPostConfigureOptions<CookieAuthenticationOptions>), ServiceLifetime.Singleton)]
internal class PostConfigureInternalCookieOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly OidcServerOptions _idsrv;
    private readonly IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> _authOptions;
    private readonly ILogger _logger;

    public PostConfigureInternalCookieOptions(
        OidcServerOptions idsrv,
        IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions> authOptions,
        ILoggerFactory loggerFactory)
    {
        _idsrv = idsrv;
        _authOptions = authOptions;
        _logger = loggerFactory.CreateLogger("MinApiLib.OidcServer.Startup");
    }

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        var scheme = _idsrv.Authentication.CookieAuthenticationScheme ??
            _authOptions.Value.DefaultAuthenticateScheme ??
            _authOptions.Value.DefaultScheme;

        if (name == scheme)
        {
            _idsrv.UserInteraction.LoginUrl = _idsrv.UserInteraction.LoginUrl ?? options.LoginPath;
            _idsrv.UserInteraction.LoginReturnUrlParameter = _idsrv.UserInteraction.LoginReturnUrlParameter ?? options.ReturnUrlParameter;
            _idsrv.UserInteraction.LogoutUrl = _idsrv.UserInteraction.LogoutUrl ?? options.LogoutPath;

            _logger.PostConfigure(
                _idsrv.UserInteraction.LoginUrl,
                _idsrv.UserInteraction.LoginReturnUrlParameter,
                _idsrv.UserInteraction.LogoutUrl,
                _idsrv.UserInteraction.ConsentUrl,
                _idsrv.UserInteraction.ConsentReturnUrlParameter,
                _idsrv.UserInteraction.ErrorUrl,
                _idsrv.UserInteraction.ErrorIdParameter);
        }
    }
}
