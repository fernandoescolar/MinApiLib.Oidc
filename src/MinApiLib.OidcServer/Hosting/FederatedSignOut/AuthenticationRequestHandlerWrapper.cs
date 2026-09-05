namespace MinApiLib.OidcServer.Hosting.FederatedSignOut;

internal class AuthenticationRequestHandlerWrapper : IAuthenticationRequestHandler
{
    private static readonly CompositeFormat IframeHtml = CompositeFormat.Parse("<iframe style='display:none' width='0' height='0' src='{0}'></iframe>");

    private readonly IAuthenticationRequestHandler _inner;
    private readonly HttpContext _context;
    private readonly ILogger? _logger;

    public AuthenticationRequestHandlerWrapper(IAuthenticationRequestHandler inner, IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _context = httpContextAccessor.HttpContext!;

        var factory = (ILoggerFactory?)_context.RequestServices.GetService(typeof(ILoggerFactory));
        _logger = factory?.CreateLogger(GetType());
    }

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        return _inner.InitializeAsync(scheme, context);
    }

    public async Task<bool> HandleRequestAsync()
    {
        var result = await _inner.HandleRequestAsync();

        if (result && _context.GetSignOutCalled() && _context.Response.StatusCode == 200)
        {
            await _context.AuthenticateAsync();

            await ProcessFederatedSignOutRequestAsync();
        }

        return result;
    }

    public Task<AuthenticateResult> AuthenticateAsync()
    {
        return _inner.AuthenticateAsync();
    }

    public Task ChallengeAsync(AuthenticationProperties? properties)
    {
        return _inner.ChallengeAsync(properties);
    }

    public Task ForbidAsync(AuthenticationProperties? properties)
    {
        return _inner.ForbidAsync(properties);
    }

    private async Task ProcessFederatedSignOutRequestAsync()
    {
        if (_logger != null) ResponseAndHostingLog.ProcessingFederatedSignOut(_logger);

        var iframeUrl = await _context.GetOidcServerSignoutFrameCallbackUrlAsync();
        if (iframeUrl != null)
        {
            if (_logger != null) ResponseAndHostingLog.RenderingSignOutCallbackIframe(_logger);
            await RenderResponseAsync(iframeUrl);
        }
        else
        {
            if (_logger != null) ResponseAndHostingLog.NoSignOutCallbackIframe(_logger);
        }
    }

    private async Task RenderResponseAsync(string iframeUrl)
    {
        _context.Response.SetNoCache();

        if (_context.Response.Body.CanWrite)
        {
            var iframe = string.Format(CultureInfo.InvariantCulture, IframeHtml, iframeUrl);
            _context.Response.ContentType = "text/html";
            await _context.Response.WriteAsync(iframe);
            await _context.Response.Body.FlushAsync();
        }
    }
}
