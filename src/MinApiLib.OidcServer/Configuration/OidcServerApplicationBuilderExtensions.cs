using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Pipeline extension methods for adding OidcServer
/// </summary>
public static class OidcServerApplicationBuilderExtensions
{
    /// <summary>
    /// Adds OidcServer to the pipeline.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static IApplicationBuilder UseOidcServer(this IApplicationBuilder app, OidcServerMiddlewareOptions? options = null)
    {
        app.Validate();

        app.UseMiddleware<BaseUrlMiddleware>();

        app.ConfigureCors();

        options ??= new OidcServerMiddlewareOptions();
        options.AuthenticationMiddleware(app);

        app.UseMiddleware<MutualTlsEndpointMiddleware>();
        app.UseMiddleware<OidcServerMiddleware>();

        return app;
    }

    internal static void Validate(this IApplicationBuilder app)
    {
        var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory
            ?? throw new InvalidOperationException("No ILoggerFactory is registered.");

        var logger = loggerFactory.CreateLogger("MinApiLib.OidcServer.Startup");
        // the SDK always stamps the assembly with AssemblyInformationalVersionAttribute
        logger.StartingOidcServer(typeof(MinApiLib.OidcServer.Hosting.OidcServerMiddleware).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);

        // IServiceScopeFactory is always registered by the DI container
        var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>()!;

        using var scope = scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        TestService(serviceProvider, typeof(IPersistedGrantStore), logger, "No storage mechanism for grants specified. Use the 'AddInMemoryPersistedGrants' extension method to register a development version.");
        TestService(serviceProvider, typeof(IClientStore), logger, "No storage mechanism for clients specified. Use the 'AddInMemoryClients' extension method to register a development version.");
        TestService(serviceProvider, typeof(IResourceStore), logger, "No storage mechanism for resources specified. Use the 'AddInMemoryIdentityResources' or 'AddInMemoryApiResources' extension method to register a development version.");

        var isDevelopment = app.ApplicationServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;

        // TestService above throws when IPersistedGrantStore is not registered
        var persistedGrants = serviceProvider.GetService(typeof(IPersistedGrantStore))!;
        if (persistedGrants.GetType().FullName == typeof(InMemoryPersistedGrantStore).FullName)
        {
            logger.UsingInMemoryPersistedGrantStore();
            if (!isDevelopment)
            {
                logger.InMemoryPersistedGrantStoreOutsideDevelopment();
            }
        }

        var options = serviceProvider.GetRequiredService<OidcServerOptions>();
        ValidateOptions(options, logger);

        if (!isDevelopment)
        {
            if (options.IssuerUri.IsMissing())
            {
                logger.IssuerUriNotConfigured();
            }

            if (serviceProvider.GetService<DeveloperSigningCredentialMarker>() != null)
            {
                logger.DeveloperSigningCredentialOutsideDevelopment();
            }
        }

        ValidateAsync(serviceProvider, logger).GetAwaiter().GetResult();
    }

    private static async Task ValidateAsync(IServiceProvider services, ILogger logger)
    {
        var options = services.GetRequiredService<OidcServerOptions>();
        var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();

        if (await schemes.GetDefaultAuthenticateSchemeAsync() == null && options.Authentication.CookieAuthenticationScheme == null)
        {
            logger.NoAuthenticationScheme();
        }
        else
        {
            AuthenticationScheme? authenticationScheme = null;

            if (options.Authentication.CookieAuthenticationScheme != null)
            {
                // a CookieAuthenticationScheme that is not registered fails here, as it always has
                authenticationScheme = (await schemes.GetSchemeAsync(options.Authentication.CookieAuthenticationScheme))!;
                logger.UsingExplicitAuthenticationScheme(authenticationScheme.Name);
            }
            else
            {
                // checked above: without CookieAuthenticationScheme a default authenticate scheme exists
                authenticationScheme = (await schemes.GetDefaultAuthenticateSchemeAsync())!;
                logger.UsingDefaultAuthenticationScheme(authenticationScheme.Name);
            }

            if (!typeof(IAuthenticationSignInHandler).IsAssignableFrom(authenticationScheme.HandlerType))
            {
                logger.AuthenticationSchemeNotSupportingSignin(authenticationScheme.Name);
            }

            logger.DefaultAuthenticationScheme((await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
            logger.DefaultSignInScheme((await schemes.GetDefaultSignInSchemeAsync())?.Name);
            logger.DefaultSignOutScheme((await schemes.GetDefaultSignOutSchemeAsync())?.Name);
            logger.DefaultChallengeScheme((await schemes.GetDefaultChallengeSchemeAsync())?.Name);
            logger.DefaultForbidScheme((await schemes.GetDefaultForbidSchemeAsync())?.Name);
        }
    }

    private static void ValidateOptions(OidcServerOptions options, ILogger logger)
    {
        if (options.IssuerUri.IsPresent()) logger.CustomIssuerUri(options.IssuerUri);

        if (options.UserInteraction.LogoutIdParameter.IsMissing()) throw new InvalidOperationException("LogoutIdParameter is not configured");
        if (options.UserInteraction.ErrorUrl.IsMissing()) throw new InvalidOperationException("ErrorUrl is not configured");
        if (options.UserInteraction.ErrorIdParameter.IsMissing()) throw new InvalidOperationException("ErrorIdParameter is not configured");
        if (options.UserInteraction.ConsentUrl.IsMissing()) throw new InvalidOperationException("ConsentUrl is not configured");
        if (options.UserInteraction.ConsentReturnUrlParameter.IsMissing()) throw new InvalidOperationException("ConsentReturnUrlParameter is not configured");
        if (options.UserInteraction.CustomRedirectReturnUrlParameter.IsMissing()) throw new InvalidOperationException("CustomRedirectReturnUrlParameter is not configured");
        if (options.Authentication.CheckSessionCookieName.IsMissing()) throw new InvalidOperationException("CheckSessionCookieName is not configured");
        if (options.Cors.CorsPolicyName.IsMissing()) throw new InvalidOperationException("CorsPolicyName is not configured");
    }

    internal static object? TestService(IServiceProvider serviceProvider, Type service, ILogger logger, string? message = null, bool doThrow = true)
    {
        var appService = serviceProvider.GetService(service);
        if (appService == null)
        {
            // service types are closed, non-generic-parameter types, so FullName is never null
            logger.RequiredServiceNotRegistered(service.FullName!);
            if (doThrow)
            {
                var error = message ?? $"Required service {service.FullName} is not registered in the DI container. Aborting startup";
                throw new InvalidOperationException(error);
            }
        }

        return appService;
    }
}
