namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering core services
/// </summary>
public static class IdentityServerBuilderExtensionsCore
{
    /// <summary>
    /// Adds the required platform services.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddRequiredPlatformServices(this IIdentityServerBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddOptions();
        builder.Services.AddSingleton(
            resolver => resolver.GetRequiredService<IOptions<IdentityServerOptions>>().Value);
        // Inconsistent options stop the host from starting instead of failing on the first request.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<IdentityServerOptions>, IdentityServerOptionsValidator>());
        builder.Services.AddOptions<IdentityServerOptions>().ValidateOnStart();
        builder.Services.AddHttpClient();
        // The protocol endpoints are served through endpoint routing (MapIdentityServer).
        builder.Services.AddRouting();

        return builder;
    }

    /// <summary>
    /// Adds the default cookie handlers and corresponding configuration
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddCookieAuthentication(this IIdentityServerBuilder builder)
    {
        builder.Services.AddAuthentication(IdentityServerConstants.DefaultCookieAuthenticationScheme)
            .AddCookie(IdentityServerConstants.DefaultCookieAuthenticationScheme)
            .AddCookie(IdentityServerConstants.ExternalCookieAuthenticationScheme);

        builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureInternalCookieOptions>();
        builder.Services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureInternalCookieOptions>();
        builder.Services.AddTransientDecorator<IAuthenticationService, IdentityServerAuthenticationService>();
        builder.Services.AddTransientDecorator<IAuthenticationHandlerProvider, FederatedSignoutAuthenticationHandlerProvider>();

        return builder;
    }

    /// <summary>
    /// Adds the default endpoints.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDefaultEndpoints(this IIdentityServerBuilder builder)
    {
        builder.Services.TryAddSingleton<IdentityServerEndpointsMarker>();

        builder.AddEndpoint<AuthorizeCallbackEndpoint>(EndpointNames.Authorize, ProtocolRoutePaths.AuthorizeCallback.EnsureLeadingSlash());
        builder.AddEndpoint<AuthorizeEndpoint>(EndpointNames.Authorize, ProtocolRoutePaths.Authorize.EnsureLeadingSlash());
        builder.AddEndpoint<CheckSessionEndpoint>(EndpointNames.CheckSession, ProtocolRoutePaths.CheckSession.EnsureLeadingSlash());
        builder.AddEndpoint<DeviceAuthorizationEndpoint>(EndpointNames.DeviceAuthorization, ProtocolRoutePaths.DeviceAuthorization.EnsureLeadingSlash());
        builder.AddEndpoint<DiscoveryKeyEndpoint>(EndpointNames.Discovery, ProtocolRoutePaths.DiscoveryWebKeys.EnsureLeadingSlash());
        builder.AddEndpoint<DiscoveryEndpoint>(EndpointNames.Discovery, ProtocolRoutePaths.DiscoveryConfiguration.EnsureLeadingSlash());
        builder.AddEndpoint<EndSessionCallbackEndpoint>(EndpointNames.EndSession, ProtocolRoutePaths.EndSessionCallback.EnsureLeadingSlash());
        builder.AddEndpoint<EndSessionEndpoint>(EndpointNames.EndSession, ProtocolRoutePaths.EndSession.EnsureLeadingSlash());
        builder.AddEndpoint<IntrospectionEndpoint>(EndpointNames.Introspection, ProtocolRoutePaths.Introspection.EnsureLeadingSlash());
        builder.AddEndpoint<TokenRevocationEndpoint>(EndpointNames.Revocation, ProtocolRoutePaths.Revocation.EnsureLeadingSlash());
        builder.AddEndpoint<TokenEndpoint>(EndpointNames.Token, ProtocolRoutePaths.Token.EnsureLeadingSlash());
        builder.AddEndpoint<UserInfoEndpoint>(EndpointNames.UserInfo, ProtocolRoutePaths.UserInfo.EnsureLeadingSlash());
        builder.AddEndpoint<NonceEndpoint>(EndpointNames.Nonce, ProtocolRoutePaths.Nonce.EnsureLeadingSlash());
        builder.AddEndpoint<PushedAuthorizationEndpoint>(EndpointNames.PushedAuthorization, ProtocolRoutePaths.PushedAuthorization.EnsureLeadingSlash());

        return builder;
    }

    /// <summary>
    /// Adds the endpoint.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The name.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddEndpoint<T>(this IIdentityServerBuilder builder, string name, PathString path)
        where T : class, IEndpointHandler
    {
        builder.Services.AddTransient<T>();
        builder.Services.AddSingleton(new IdentityServer.Hosting.Endpoint(name, path, typeof(T)));

        return builder;
    }

    /// <summary>
    /// Adds the core services.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddCoreServices(this IIdentityServerBuilder builder)
    {
        builder.Services.AddTransient<ISecretsListParser, SecretParser>();
        builder.Services.AddTransient<ISecretsListValidator, SecretValidator>();
        builder.Services.AddTransient<ExtensionGrantValidator>();
        builder.Services.AddTransient<BearerTokenUsageValidator>();
        builder.Services.AddTransient<JwtRequestValidator>();

        builder.Services.AddTransient<ReturnUrlParser>();
        builder.Services.AddTransient<IdentityServerTools>();
        builder.Services.TryAddTransient<IIssuerNameService, DefaultIssuerNameService>();

        builder.Services.AddTransient<IReturnUrlParser, OidcReturnUrlParser>();
        builder.Services.AddScoped<IUserSession, DefaultUserSession>();
        builder.Services.AddTransient(typeof(MessageCookie<>));

        builder.Services.AddCors();
        builder.Services.AddTransientDecorator<ICorsPolicyProvider, CorsPolicyProvider>();

        return builder;
    }

    /// <summary>
    /// Adds the pluggable services.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddPluggableServices(this IIdentityServerBuilder builder)
    {
        builder.Services.TryAddTransient<IPersistedGrantService, DefaultPersistedGrantService>();
        builder.Services.TryAddTransient<IKeyMaterialService, DefaultKeyMaterialService>();
        builder.Services.AddDataProtection();
        builder.Services.TryAddSingleton<ISigningKeyStore, FileSystemSigningKeyStore>();
        builder.Services.TryAddSingleton<ISigningKeyManager, AutomaticKeyManager>();
        builder.Services.TryAddTransient<ITokenService, DefaultTokenService>();
        builder.Services.TryAddTransient<ITokenCreationService, DefaultTokenCreationService>();
        builder.Services.TryAddTransient<IClaimsService, DefaultClaimsService>();
        builder.Services.TryAddTransient<IRefreshTokenService, DefaultRefreshTokenService>();
        builder.Services.TryAddTransient<IDeviceFlowCodeService, DefaultDeviceFlowCodeService>();
        builder.Services.TryAddTransient<IConsentService, DefaultConsentService>();
        builder.Services.TryAddTransient<ICorsPolicyService, DefaultCorsPolicyService>();
        builder.Services.TryAddTransient<IProfileService, DefaultProfileService>();
        builder.Services.TryAddTransient<IConsentMessageStore, ConsentMessageStore>();
        builder.Services.TryAddTransient<IMessageStore<LogoutMessage>, ProtectedDataMessageStore<LogoutMessage>>();
        builder.Services.TryAddTransient<IMessageStore<LogoutNotificationContext>, ProtectedDataMessageStore<LogoutNotificationContext>>();
        builder.Services.TryAddTransient<IMessageStore<ErrorMessage>, ProtectedDataMessageStore<ErrorMessage>>();
        builder.Services.TryAddTransient<IIdentityServerInteractionService, DefaultIdentityServerInteractionService>();
        builder.Services.TryAddTransient<IDeviceFlowInteractionService, DefaultDeviceFlowInteractionService>();
        builder.Services.TryAddTransient<IAuthorizationCodeStore, DefaultAuthorizationCodeStore>();
        builder.Services.TryAddTransient<IAuthorizationNonceStore, DefaultAuthorizationNonceStore>();
        builder.Services.TryAddTransient<IPushedAuthorizationRequestStore, DefaultPushedAuthorizationRequestStore>();
        builder.Services.TryAddTransient<IDPoPProofValidator, DefaultDPoPProofValidator>();
        builder.Services.TryAddTransient<IReplayCache, DefaultReplayCache>();
        builder.Services.TryAddTransient<IRefreshTokenStore, DefaultRefreshTokenStore>();
        builder.Services.TryAddTransient<IReferenceTokenStore, DefaultReferenceTokenStore>();
        builder.Services.TryAddTransient<IUserConsentStore, DefaultUserConsentStore>();
        builder.Services.TryAddTransient<IHandleGenerationService, DefaultHandleGenerationService>();
        builder.Services.TryAddTransient<IPersistentGrantSerializer, PersistentGrantSerializer>();
        builder.Services.TryAddTransient<IEventService, DefaultEventService>();
        builder.Services.TryAddTransient<IEventSink, DefaultEventSink>();
        builder.Services.TryAddTransient<IUserCodeService, DefaultUserCodeService>();
        builder.Services.TryAddTransient<IUserCodeGenerator, NumericUserCodeGenerator>();
        builder.Services.TryAddTransient<ILogoutNotificationService, LogoutNotificationService>();
        builder.Services.TryAddTransient<IBackChannelLogoutService, DefaultBackChannelLogoutService>();
        builder.Services.TryAddSingleton<BackChannelLogoutQueue>();
        builder.Services.AddHostedService<BackChannelLogoutBackgroundService>();
        builder.Services.TryAddTransient<IResourceValidator, DefaultResourceValidator>();
        builder.Services.TryAddTransient<IScopeParser, DefaultScopeParser>();

        builder.AddJwtRequestUriHttpClient();
        builder.AddBackChannelLogoutHttpClient();

        builder.Services.AddTransient<IClientSecretValidator, ClientSecretValidator>();
        builder.Services.AddTransient<IApiSecretValidator, ApiSecretValidator>();

        builder.Services.TryAddTransient<IDeviceFlowThrottlingService, DistributedDeviceFlowThrottlingService>();
        builder.Services.AddDistributedMemoryCache();

        return builder;
    }

    /// <summary>
    /// Adds the validators.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddValidators(this IIdentityServerBuilder builder)
    {
        builder.Services.TryAddTransient<IEndSessionRequestValidator, EndSessionRequestValidator>();
        builder.Services.TryAddTransient<ITokenRevocationRequestValidator, TokenRevocationRequestValidator>();
        builder.Services.TryAddTransient<IAuthorizeRequestValidator, AuthorizeRequestValidator>();
        builder.Services.TryAddTransient<ITokenRequestValidator, TokenRequestValidator>();
        builder.Services.TryAddTransient<IRedirectUriValidator, StrictRedirectUriValidator>();
        builder.Services.TryAddTransient<ITokenValidator, TokenValidator>();
        builder.Services.TryAddTransient<IIntrospectionRequestValidator, IntrospectionRequestValidator>();
        builder.Services.TryAddTransient<ICustomTokenRequestValidator, DefaultCustomTokenRequestValidator>();
        builder.Services.TryAddTransient<IUserInfoRequestValidator, UserInfoRequestValidator>();
        builder.Services.TryAddTransient<IClientConfigurationValidator, DefaultClientConfigurationValidator>();
        builder.Services.TryAddTransient<IDeviceAuthorizationRequestValidator, DeviceAuthorizationRequestValidator>();
        builder.Services.TryAddTransient<IDeviceCodeValidator, DeviceCodeValidator>();
        builder.Services.TryAddTransient<IResourceOwnerPasswordValidator, NotSupportedResourceOwnerPasswordValidator>();
        builder.Services.TryAddTransient<INonceRequestValidator, NonceRequestValidator>();

        builder.Services.TryAddTransient<ICustomTokenValidator, DefaultCustomTokenValidator>();
        builder.Services.TryAddTransient<ICustomAuthorizeRequestValidator, DefaultCustomAuthorizeRequestValidator>();

        return builder;
    }

    /// <summary>
    /// Adds the response generators.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddResponseGenerators(this IIdentityServerBuilder builder)
    {
        builder.Services.TryAddTransient<ITokenResponseGenerator, TokenResponseGenerator>();
        builder.Services.TryAddTransient<IUserInfoResponseGenerator, UserInfoResponseGenerator>();
        builder.Services.TryAddTransient<IIntrospectionResponseGenerator, IntrospectionResponseGenerator>();
        builder.Services.TryAddTransient<IAuthorizeInteractionResponseGenerator, AuthorizeInteractionResponseGenerator>();
        builder.Services.TryAddTransient<IAuthorizeResponseGenerator, AuthorizeResponseGenerator>();
        builder.Services.TryAddTransient<IDiscoveryResponseGenerator, DiscoveryResponseGenerator>();
        builder.Services.TryAddTransient<ITokenRevocationResponseGenerator, TokenRevocationResponseGenerator>();
        builder.Services.TryAddTransient<IDeviceAuthorizationResponseGenerator, DeviceAuthorizationResponseGenerator>();
        builder.Services.TryAddTransient<INonceResponseGenerator, NonceResponseGenerator>();

        return builder;
    }

    /// <summary>
    /// Adds the default secret parsers.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDefaultSecretParsers(this IIdentityServerBuilder builder)
    {
        builder.Services.AddTransient<ISecretParser, BasicAuthenticationSecretParser>();
        builder.Services.AddTransient<ISecretParser, PostBodySecretParser>();

        return builder;
    }

    /// <summary>
    /// Adds the default secret validators.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDefaultSecretValidators(this IIdentityServerBuilder builder)
    {
        builder.Services.AddTransient<ISecretValidator, HashedSharedSecretValidator>();

        return builder;
    }
}
