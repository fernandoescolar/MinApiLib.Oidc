namespace MinApiLib.Oidc.Server.Core.Configuration;

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
            options.ReturnUrlParameter = _idsrv.UserInteraction.LoginReturnUrlParameter;
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
