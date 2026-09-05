using IdentityServer.Configuration.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to add ASP.NET Identity support to IdentityServer.
/// </summary>
public static class IdentityServerBuilderExtensions
{
    /// <summary>
    /// Configures IdentityServer to use the ASP.NET Identity implementations
    /// of IUserClaimsPrincipalFactory and IProfileService.
    /// Also configures some of ASP.NET Identity's options for use with IdentityServer (such as claim types to use
    /// and authentication cookie settings).
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddAspNetIdentity<TUser>(this IIdentityServerBuilder builder)
        where TUser : class
    {
        builder.Services.AddTransientDecorator<IUserClaimsPrincipalFactory<TUser>, UserClaimsFactory<TUser>>();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
            options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
            options.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
        });

        builder.Services.Configure<SecurityStampValidatorOptions>(opts =>
        {
            opts.OnRefreshingPrincipal = SecurityStampValidatorCallback.UpdatePrincipal;
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = AspNetCore.Http.SameSiteMode.None;
        });

        builder.Services.ConfigureExternalCookie(options =>
        {
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = AspNetCore.Http.SameSiteMode.None;
        });

        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorRememberMeScheme, options =>
        {
            options.Cookie.IsEssential = true;
        });

        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddAuthentication(options =>
        {
            if (options.DefaultAuthenticateScheme == null &&
                options.DefaultScheme == IdentityServerConstants.DefaultCookieAuthenticationScheme)
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
            }
        });

        builder.AddResourceOwnerValidator<ResourceOwnerPasswordValidator<TUser>>();
        builder.AddProfileService<ProfileService<TUser>>();

        return builder;
    }
}
