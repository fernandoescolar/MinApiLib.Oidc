namespace IdentityServer.UnitTests.Common;

internal class MockHttpContextAccessor : IHttpContextAccessor
{
    private HttpContext _context = new DefaultHttpContext();
    public MockAuthenticationService AuthenticationService { get; set; } = new MockAuthenticationService();

    public MockAuthenticationSchemeProvider Schemes { get; set; } = new MockAuthenticationSchemeProvider();

    public MockHttpContextAccessor(
        IdentityServerOptions options = null,
        IUserSession userSession = null,
        IMessageStore<LogoutNotificationContext> endSessionStore = null)
    {
        options ??= TestIdentityServerOptions.Create();

        var timeProvider = TimeProvider.System;
        var loggerFactory = NullLoggerFactory.Instance;

        if (userSession == null)
        {
            userSession = new DefaultUserSession(
                this,
                new MockAuthenticationHandlerProvider(),
                options,
                timeProvider,
                loggerFactory.CreateLogger<IUserSession>());
        }

        if (endSessionStore == null)
        {
            var dataProtectionProvider = new EphemeralDataProtectionProvider(loggerFactory);
            endSessionStore = new ProtectedDataMessageStore<LogoutNotificationContext>(
                dataProtectionProvider,
                loggerFactory.CreateLogger<ProtectedDataMessageStore<LogoutNotificationContext>>());
        }

        var serviceProvider = new SimpleServiceProvider();
        serviceProvider.Add<IdentityServerOptions>(options);
        serviceProvider.Add<IAuthenticationSchemeProvider>(Schemes);
        serviceProvider.Add<IAuthenticationService>(AuthenticationService);
        serviceProvider.Add<IUserSession>(userSession);
        serviceProvider.Add<IMessageStore<LogoutNotificationContext>>(endSessionStore);
        serviceProvider.Add<TimeProvider>(timeProvider);

        _context.RequestServices = serviceProvider;
    }

    public HttpContext HttpContext
    {
        get
        {
            return _context;
        }

        set
        {
            _context = value;
        }
    }
}
