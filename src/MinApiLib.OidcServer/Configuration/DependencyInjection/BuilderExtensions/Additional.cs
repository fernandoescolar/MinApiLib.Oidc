namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering additional services
/// </summary>
public static class OidcServerBuilderExtensionsAdditional
{
    /// <summary>
    /// Adds the extension grant validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddExtensionGrantValidator<T>(this IOidcServerBuilder builder)
        where T : class, IExtensionGrantValidator
    {
        builder.Services.AddTransient<IExtensionGrantValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds a redirect URI validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddRedirectUriValidator<T>(this IOidcServerBuilder builder)
        where T : class, IRedirectUriValidator
    {
        builder.Services.AddTransient<IRedirectUriValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds a an "AppAuth" (OAuth 2.0 for Native Apps) compliant redirect URI validator (does strict validation but also allows http://127.0.0.1 with random port)
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddAppAuthRedirectUriValidator(this IOidcServerBuilder builder)
    {
        return builder.AddRedirectUriValidator<StrictRedirectUriValidatorAppAuth>();
    }

    /// <summary>
    /// Adds the profile service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddProfileService<T>(this IOidcServerBuilder builder)
        where T : class, IProfileService
    {
        builder.Services.AddTransient<IProfileService, T>();

        return builder;
    }

    /// <summary>
    /// Adds a resource validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddResourceValidator<T>(this IOidcServerBuilder builder)
        where T : class, IResourceValidator
    {
        builder.Services.AddTransient<IResourceValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds a scope parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddScopeParser<T>(this IOidcServerBuilder builder)
        where T : class, IScopeParser
    {
        builder.Services.AddTransient<IScopeParser, T>();

        return builder;
    }

    /// <summary>
    /// Adds a client store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddClientStore<T>(this IOidcServerBuilder builder)
        where T : class, IClientStore
    {
        builder.Services.TryAddTransient<T>();
        builder.Services.AddTransient<IClientStore, ValidatingClientStore<T>>();

        return builder;
    }

    /// <summary>
    /// Adds a resource store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddResourceStore<T>(this IOidcServerBuilder builder)
        where T : class, IResourceStore
    {
        builder.Services.AddTransient<IResourceStore, T>();

        return builder;
    }

    /// <summary>
    /// Adds a device flow store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    public static IOidcServerBuilder AddDeviceFlowStore<T>(this IOidcServerBuilder builder)
        where T : class, IDeviceFlowStore
    {
        builder.Services.AddTransient<IDeviceFlowStore, T>();

        return builder;
    }

    /// <summary>
    /// Adds a persisted grant store.
    /// </summary>
    /// <typeparam name="T">The type of the concrete grant store that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder.</returns>
    public static IOidcServerBuilder AddPersistedGrantStore<T>(this IOidcServerBuilder builder)
        where T : class, IPersistedGrantStore
    {
        builder.Services.AddTransient<IPersistedGrantStore, T>();

        return builder;
    }

    /// <summary>
    /// Adds a CORS policy service.
    /// </summary>
    /// <typeparam name="T">The type of the concrete scope store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddCorsPolicyService<T>(this IOidcServerBuilder builder)
        where T : class, ICorsPolicyService
    {
        builder.Services.AddTransient<ICorsPolicyService, T>();
        return builder;
    }

    /// <summary>
    /// Adds a CORS policy service cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete CORS policy service that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddCorsPolicyCache<T>(this IOidcServerBuilder builder)
        where T : class, ICorsPolicyService
    {
        builder.Services.TryAddTransient<T>();
        builder.Services.AddTransient<ICorsPolicyService, CachingCorsPolicyService<T>>();
        return builder;
    }

    /// <summary>
    /// Adds the secret parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddSecretParser<T>(this IOidcServerBuilder builder)
        where T : class, ISecretParser
    {
        builder.Services.AddTransient<ISecretParser, T>();

        return builder;
    }

    /// <summary>
    /// Adds the secret validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddSecretValidator<T>(this IOidcServerBuilder builder)
        where T : class, ISecretValidator
    {
        builder.Services.AddTransient<ISecretValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds the client store cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete client store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddClientStoreCache<T>(this IOidcServerBuilder builder)
        where T : IClientStore
    {
        builder.Services.TryAddTransient(typeof(T));
        builder.Services.AddTransient<ValidatingClientStore<T>>();
        builder.Services.AddTransient<IClientStore, CachingClientStore<ValidatingClientStore<T>>>();

        return builder;
    }

    /// <summary>
    /// Adds the client store cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete scope store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddResourceStoreCache<T>(this IOidcServerBuilder builder)
        where T : IResourceStore
    {
        builder.Services.TryAddTransient(typeof(T));
        builder.Services.AddTransient<IResourceStore, CachingResourceStore<T>>();
        return builder;
    }

    /// <summary>
    /// Adds the authorize interaction response generator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddAuthorizeInteractionResponseGenerator<T>(this IOidcServerBuilder builder)
        where T : class, IAuthorizeInteractionResponseGenerator
    {
        builder.Services.AddTransient<IAuthorizeInteractionResponseGenerator, T>();

        return builder;
    }

    /// <summary>
    /// Adds a resource owner password validator for the deprecated password grant. The grant still only works for
    /// clients with <see cref="Client.AllowResourceOwnerPasswordFlow"/>.
    /// </summary>
    /// <typeparam name="T">The validator type.</typeparam>
    /// <param name="builder">The builder.</param>
    public static IOidcServerBuilder AddResourceOwnerValidator<T>(this IOidcServerBuilder builder)
        where T : class, IResourceOwnerPasswordValidator
    {
        builder.Services.AddTransient<IResourceOwnerPasswordValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds a handler for a custom <c>prompt</c> value and adds the value to
    /// <see cref="UserInteractionOptions.PromptValuesSupported"/>.
    /// </summary>
    /// <typeparam name="T">The handler type.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="prompt">The prompt value, for example <c>create</c> or <c>register</c>.</param>
    public static IOidcServerBuilder AddPromptHandler<T>(this IOidcServerBuilder builder, string prompt)
        where T : class, IAuthorizePromptHandler
    {
        ValidateCustomPrompt(prompt);

        builder.Services.AddKeyedTransient<IAuthorizePromptHandler, T>(prompt);
        builder.Services.Configure<OidcServerOptions>(options => options.UserInteraction.PromptValuesSupported.Add(prompt));

        return builder;
    }

    /// <summary>
    /// Sends the user to <paramref name="url"/> when the authorize request carries the <c>prompt</c> value
    /// <paramref name="prompt"/>. The page receives the authorize request as return URL, in the
    /// <see cref="UserInteractionOptions.CustomRedirectReturnUrlParameter"/> query parameter.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="prompt">The prompt value, for example <c>create</c> or <c>register</c>.</param>
    /// <param name="url">The page URL. If a local URL, the value must start with a leading slash.</param>
    public static IOidcServerBuilder AddPromptRedirect(this IOidcServerBuilder builder, string prompt, string url)
    {
        ValidateCustomPrompt(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        builder.Services.AddKeyedSingleton<IAuthorizePromptHandler>(prompt, new RedirectPromptHandler(url));
        builder.Services.Configure<OidcServerOptions>(options => options.UserInteraction.PromptValuesSupported.Add(prompt));

        return builder;
    }

    private static void ValidateCustomPrompt(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        if (prompt.Contains(' ', StringComparison.Ordinal))
        {
            throw new ArgumentException("A prompt value cannot contain spaces.", nameof(prompt));
        }

        if (AuthorizeInteractionResponseGenerator.BuiltInPromptValues.Contains(prompt))
        {
            throw new ArgumentException($"'{prompt}' is a built-in prompt value and cannot have a custom handler.", nameof(prompt));
        }
    }

    /// <summary>
    /// Adds the custom authorize request validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddCustomAuthorizeRequestValidator<T>(this IOidcServerBuilder builder)
        where T : class, ICustomAuthorizeRequestValidator
    {
        builder.Services.AddTransient<ICustomAuthorizeRequestValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds the custom authorize request validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddCustomTokenRequestValidator<T>(this IOidcServerBuilder builder)
        where T : class, ICustomTokenRequestValidator
    {
        builder.Services.AddTransient<ICustomTokenRequestValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds support for client authentication using JWT bearer assertions.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddJwtBearerClientAuthentication(this IOidcServerBuilder builder)
    {
        builder.Services.TryAddTransient<IReplayCache, DefaultReplayCache>();
        builder.AddSecretParser<JwtBearerClientAssertionSecretParser>();
        builder.AddSecretValidator<PrivateKeyJwtSecretValidator>();

        return builder;
    }

    /// <summary>
    /// Adds a client configuration validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddClientConfigurationValidator<T>(this IOidcServerBuilder builder)
        where T : class, IClientConfigurationValidator
    {
        builder.Services.AddTransient<IClientConfigurationValidator, T>();

        return builder;
    }

    /// <summary>
    /// Adds the X509 secret validators for mutual TLS.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddMutualTlsSecretValidators(this IOidcServerBuilder builder)
    {
        builder.AddSecretParser<MutualTlsSecretParser>();
        builder.AddSecretValidator<X509ThumbprintSecretValidator>();
        builder.AddSecretValidator<X509NameSecretValidator>();

        return builder;
    }

    /// <summary>
    /// Adds a custom back-channel logout service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddBackChannelLogoutService<T>(this IOidcServerBuilder builder)
        where T : class, IBackChannelLogoutService
    {
        builder.Services.AddTransient<IBackChannelLogoutService, T>();

        return builder;
    }

    /// <summary>
    /// Adds configuration for the HttpClient used for back-channel logout notifications.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureClient">The configruation callback.</param>
    /// <returns></returns>
    public static IHttpClientBuilder AddBackChannelLogoutHttpClient(this IOidcServerBuilder builder, Action<HttpClient>? configureClient = null)
    {
        const string name = OidcServerConstants.HttpClients.BackChannelLogoutHttpClient;
        IHttpClientBuilder httpBuilder;

        if (configureClient != null)
        {
            httpBuilder = builder.Services.AddHttpClient(name, configureClient);
        }
        else
        {
            httpBuilder = builder.Services.AddHttpClient(name)
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(OidcServerConstants.HttpClients.DefaultTimeoutSeconds);
                });
        }

        builder.Services.AddTransient<IBackChannelLogoutHttpClient>(s =>
        {
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(name);
            var loggerFactory = s.GetRequiredService<ILoggerFactory>();

            return new DefaultBackChannelLogoutHttpClient(httpClient, loggerFactory);
        });

        return httpBuilder;
    }

    /// <summary>
    /// Adds configuration for the HttpClient used for JWT request_uri requests.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureClient">The configruation callback.</param>
    /// <returns></returns>
    public static IHttpClientBuilder AddJwtRequestUriHttpClient(this IOidcServerBuilder builder, Action<HttpClient>? configureClient = null)
    {
        const string name = OidcServerConstants.HttpClients.JwtRequestUriHttpClient;
        IHttpClientBuilder httpBuilder;

        if (configureClient != null)
        {
            httpBuilder = builder.Services.AddHttpClient(name, configureClient);
        }
        else
        {
            httpBuilder = builder.Services.AddHttpClient(name)
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(OidcServerConstants.HttpClients.DefaultTimeoutSeconds);
                });
        }

        // No redirects and no connections to private or reserved addresses (SSRF protection).
        httpBuilder.ConfigurePrimaryHttpMessageHandler(JwtRequestUriHttpMessageHandler.Create);

        builder.Services.AddTransient<IJwtRequestUriHttpClient, DefaultJwtRequestUriHttpClient>(s =>
        {
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(name);
            var loggerFactory = s.GetRequiredService<ILoggerFactory>();
            var options = s.GetRequiredService<OidcServerOptions>();

            return new DefaultJwtRequestUriHttpClient(httpClient, options, loggerFactory);
        });

        return httpBuilder;
    }

    /// <summary>
    /// Adds a custom authorization request parameter store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddAuthorizationParametersMessageStore<T>(this IOidcServerBuilder builder)
        where T : class, IAuthorizationParametersMessageStore
    {
        builder.Services.AddTransient<IAuthorizationParametersMessageStore, T>();

        return builder;
    }

    /// <summary>
    /// Adds a custom user session.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddUserSession<T>(this IOidcServerBuilder builder)
        where T : class, IUserSession
    {
        builder.Services.AddScoped<IUserSession, T>();

        return builder;
    }
}
