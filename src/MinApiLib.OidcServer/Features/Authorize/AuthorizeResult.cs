namespace MinApiLib.OidcServer.Features.Authorize;

internal class AuthorizeResult : IEndpointResult
{
    public AuthorizeResponse Response { get; }

    public AuthorizeResult(AuthorizeResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    internal AuthorizeResult(
        AuthorizeResponse response,
        OidcServerOptions options,
        IUserSession userSession,
        IMessageStore<ErrorMessage> errorMessageStore,
        TimeProvider clock)
        : this(response)
    {
        _options = options;
        _userSession = userSession;
        _errorMessageStore = errorMessageStore;
        _clock = clock;
    }

    private OidcServerOptions _options = null!;
    private IUserSession _userSession = null!;
    private IMessageStore<ErrorMessage> _errorMessageStore = null!;
    private TimeProvider _clock = null!;

    private void Init(HttpContext context)
    {
        _options = _options ?? context.RequestServices.GetRequiredService<OidcServerOptions>();
        _userSession = _userSession ?? context.RequestServices.GetRequiredService<IUserSession>();
        _errorMessageStore = _errorMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();
        _clock = _clock ?? context.RequestServices.GetRequiredService<TimeProvider>();
    }

    public Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        if (Response.IsError)
        {
            return ProcessErrorAsync(context);
        }
        else
        {
            return ProcessResponseAsync(context);
        }
    }

    private Task ProcessErrorAsync(HttpContext context)
    {
        var isSafeError =
            Response.Error is OidcConstants.AuthorizeErrors.AccessDenied or
            OidcConstants.AuthorizeErrors.AccountSelectionRequired or
            OidcConstants.AuthorizeErrors.LoginRequired or
            OidcConstants.AuthorizeErrors.ConsentRequired or
            OidcConstants.AuthorizeErrors.InteractionRequired;

        if (isSafeError)
        {
            return ProcessResponseAsync(context);
        }
        else
        {
            return RedirectToErrorPageAsync(context);
        }
    }

    protected async Task ProcessResponseAsync(HttpContext context)
    {
        if (!Response.IsError)
        {
            await _userSession.AddClientIdAsync(Response.Request.ClientId);
        }

        await RenderAuthorizeResponseAsync(context);
    }

    private async Task RenderAuthorizeResponseAsync(HttpContext context)
    {
        if (_options.EmitIssuerIdentificationResponseParameter)
        {
            Response.Issuer = context.GetOidcServerIssuerUri();
        }

        if (Response.Request.ResponseMode is OidcConstants.ResponseModes.Query or
            OidcConstants.ResponseModes.Fragment)
        {
            context.Response.SetNoCache();
            context.Response.RedirectSeeOtherAfterPost(BuildRedirectUri());
        }
        else if (Response.Request.ResponseMode == OidcConstants.ResponseModes.FormPost)
        {
            context.Response.SetNoCache();
            AddSecurityHeaders(context);
            await context.Response.WriteHtmlAsync(GetFormPostHtml());
        }
        else
        {
            throw new InvalidOperationException("Unsupported response mode");
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        context.Response.AddScriptCspHeaders(_options.Csp, "sha256-orD0/VhH8hLqrLxKHD/HUEMdwqX6/0ve7c5hspX5VJ8=");

        var referrer_policy = "no-referrer";
        if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
        {
            context.Response.Headers.Append("Referrer-Policy", referrer_policy);
        }
    }

    private string BuildRedirectUri()
    {
        var uri = Response.Request.RedirectUri;
        var query = Response.ToNameValueCollection().ToQueryString();

        if (Response.Request.ResponseMode == OidcConstants.ResponseModes.Query)
        {
            uri = uri.AddQueryString(query);
        }
        else
        {
            uri = uri.AddHashFragment(query);
        }

        if (Response.IsError && !uri.Contains('#'))
        {
            uri += "#_=_";
        }

        return uri;
    }

    private const string FormPostHtml = "<html><head><meta http-equiv='X-UA-Compatible' content='IE=edge' /><base target='_self'/></head><body><form method='post' action='{uri}'>{body}<noscript><button>Click to continue</button></noscript></form><script>window.addEventListener('load', function(){document.forms[0].submit();});</script></body></html>";

    private string GetFormPostHtml()
    {
        var html = FormPostHtml;

        var url = Response.Request.RedirectUri;
        url = HtmlEncoder.Default.Encode(url);
        html = html.Replace("{uri}", url);
        html = html.Replace("{body}", Response.ToNameValueCollection().ToFormPost());

        return html;
    }

    private async Task RedirectToErrorPageAsync(HttpContext context)
    {
        var errorModel = new ErrorMessage
        {
            RequestId = context.TraceIdentifier,
            Error = Response.Error ?? OidcConstants.AuthorizeErrors.ServerError,
            ErrorDescription = Response.ErrorDescription,
            UiLocales = Response.Request?.UiLocales,
            DisplayMode = Response.Request?.DisplayMode,
            ClientId = Response.Request?.ClientId
        };

        var message = new Message<ErrorMessage>(errorModel, _clock.GetUtcNow().UtcDateTime);
        var id = await _errorMessageStore.WriteAsync(message)
            ?? throw new InvalidOperationException("The error message could not be stored.");

        var errorUrl = _options.UserInteraction.ErrorUrl;

        var url = errorUrl.AddQueryString(_options.UserInteraction.ErrorIdParameter, id);
        context.Response.RedirectToAbsoluteUrl(url);
    }
}
