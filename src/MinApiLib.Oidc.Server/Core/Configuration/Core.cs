// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering core services
/// </summary>
/// <remarks>
/// The default implementations of the OidcServer services are marked with MinApiLib's
/// <c>[RegisterInIServiceCollection]</c> attribute and registered by <see cref="AddDefaultServices"/>, which scans
/// this assembly. Only what an attribute cannot express (options, authentication, decorators, HTTP clients, hosted
/// services and framework services) is registered here by hand.
/// </remarks>
public static class OidcServerBuilderExtensionsCore
{
    /// <summary>
    /// Adds the required platform services.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddRequiredPlatformServices(this IOidcServerBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddOptions();
        builder.Services.AddSingleton(
            resolver => resolver.GetRequiredService<IOptions<OidcServerOptions>>().Value);
        // Inconsistent options stop the host from starting instead of failing on the first request.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OidcServerOptions>, OidcServerOptionsValidator>());
        builder.Services.AddOptions<OidcServerOptions>().ValidateOnStart();
        builder.Services.AddHttpClient();
        // The protocol endpoints are MinApiLib endpoints served through endpoint routing (MapOidcServer).
        builder.Services.AddRouting();

        return builder;
    }

    /// <summary>
    /// Adds the default cookie handlers. Their configuration is registered by <see cref="AddDefaultServices"/>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddCookieAuthentication(this IOidcServerBuilder builder)
    {
        builder.Services.AddAuthentication(OidcServerConstants.DefaultCookieAuthenticationScheme)
            .AddCookie(OidcServerConstants.DefaultCookieAuthenticationScheme)
            .AddCookie(OidcServerConstants.ExternalCookieAuthenticationScheme);

        builder.Services.AddTransientDecorator<IAuthenticationService, OidcServerAuthenticationService>();
        builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, FederatedSignoutAuthenticationHandlerProvider>();

        return builder;
    }

    /// <summary>
    /// Adds the default implementation of every OidcServer service.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    /// <remarks>
    /// The services marked with <c>[RegisterInIServiceCollection]</c> are found by MinApiLib's <c>AddAssembly</c> and
    /// tried, not added: a service the host registered before <c>AddOidcServer()</c> keeps its implementation, and one
    /// registered after it (as the builder's <c>Add*</c> methods do) is the one the container resolves. The services
    /// with several implementations, secret parsers and validators, return URL parsers and options setups, are added
    /// next to the host's.
    /// </remarks>
    public static IOidcServerBuilder AddDefaultServices(this IOidcServerBuilder builder)
    {
        var defaults = new ServiceCollection();
        defaults.AddAssembly(typeof(OidcServerOptions).Assembly);
        foreach (var descriptor in defaults)
        {
            if (IsEnumerable(descriptor.ServiceType))
            {
                builder.Services.TryAddEnumerable(descriptor);
            }
            else
            {
                builder.Services.TryAdd(descriptor);
            }
        }

        // StrictRedirectUriValidatorAppAuth derives from it and would inherit the attribute.
        builder.Services.TryAddTransient<IRedirectUriValidator, StrictRedirectUriValidator>();

        builder.Services.AddCors();
        builder.Services.AddTransientDecorator<ICorsPolicyProvider, CorsPolicyProvider>();

        builder.Services.AddDataProtection();
        builder.Services.AddHostedService<BackChannelLogoutBackgroundService>();
        builder.Services.AddDistributedMemoryCache();

        builder.AddJwtRequestUriHttpClient();
        builder.AddBackChannelLogoutHttpClient();

        return builder;
    }

    private static bool IsEnumerable(Type serviceType)
    {
        if (serviceType == typeof(ISecretParser) || serviceType == typeof(ISecretValidator) || serviceType == typeof(IReturnUrlParser))
        {
            return true;
        }

        if (!serviceType.IsGenericType)
        {
            return false;
        }

        var definition = serviceType.GetGenericTypeDefinition();
        return definition == typeof(IConfigureOptions<>) || definition == typeof(IPostConfigureOptions<>);
    }
}
