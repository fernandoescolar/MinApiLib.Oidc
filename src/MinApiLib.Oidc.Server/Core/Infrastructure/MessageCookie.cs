namespace MinApiLib.Oidc.Server.Core.Infrastructure;

[RegisterInIServiceCollection]
internal class MessageCookie<TModel>
{
    private readonly ILogger _logger;
    private readonly OidcServerOptions _options;
    private readonly IHttpContextAccessor _context;
    private readonly IDataProtector _protector;

    public MessageCookie(
        ILogger<MessageCookie<TModel>> logger,
        OidcServerOptions options,
        IHttpContextAccessor context,
        IDataProtectionProvider provider)
    {
        _logger = logger;
        _options = options;
        _context = context;
        _protector = provider.CreateProtector(MessageType);
    }

    private string MessageType => typeof(TModel).Name;

    // only used while handling a request, so HttpContext is always available
    private HttpContext HttpContext => _context.HttpContext!;

    private string Protect(Message<TModel> message)
    {
        var json = ObjectSerializer.ToString(message);
        SecretLog.ProtectingMessage(_logger, json);

        return _protector.Protect(json);
    }

    private Message<TModel>? Unprotect(string data)
    {
        var json = _protector.Unprotect(data);
        var message = ObjectSerializer.FromString<Message<TModel>>(json);
        return message;
    }

    private string CookiePrefix => MessageType + ".";

    private string GetCookieFullName(string id)
    {
        return CookiePrefix + id;
    }

    private string CookiePath => HttpContext.GetOidcServerBasePath().CleanUrlPath();

    private IEnumerable<string> GetCookieNames()
    {
        var key = CookiePrefix;
        foreach ((string name, var _) in HttpContext.Request.Cookies)
        {
            if (name.StartsWith(key, StringComparison.Ordinal))
            {
                yield return name;
            }
        }
    }

    private bool Secure => HttpContext.Request.IsHttps;

    public void Write(string id, Message<TModel> message)
    {
        ClearOverflow();

        ArgumentNullException.ThrowIfNull(message);

        var name = GetCookieFullName(id);
        var data = Protect(message);

        HttpContext.Response.Cookies.Append(
            name,
            data,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Secure,
                Path = CookiePath,
                IsEssential = true
                // don't need to set same-site since cookie is expected to be sent
                // to only another page in this host.
            });
    }

    public Message<TModel>? Read(string id)
    {
        if (id.IsMissing()) return null;

        var name = GetCookieFullName(id);
        return ReadByCookieName(name);
    }

    private Message<TModel>? ReadByCookieName(string name)
    {
        var data = HttpContext.Request.Cookies[name];
        if (data.IsPresent())
        {
            try
            {
                return Unprotect(data);
            }
            catch (Exception ex)
            {
                SecretLog.MessageCookieUnprotectFailed(_logger, ex);
                ClearByCookieName(name);
            }
        }
        return null;
    }

    protected internal void Clear(string id)
    {
        var name = GetCookieFullName(id);
        ClearByCookieName(name);
    }

    private void ClearByCookieName(string name)
    {
        HttpContext.Response.Cookies.Append(
            name,
            ".",
            new CookieOptions
            {
                Expires = new DateTime(2000, 1, 1),
                HttpOnly = true,
                Secure = Secure,
                Path = CookiePath,
                IsEssential = true
            });
    }

    private long GetCookieRank(string name)
    {
        // empty and invalid cookies are considered to be the oldest:
        var rank = DateTime.MinValue.Ticks;

        try
        {
            var message = ReadByCookieName(name);
            if (message != null)
            {
                // valid cookies are ranked based on their creation time:
                rank = message.Created;
            }
        }
        catch (CryptographicException e)
        {
            // cookie was protected with a different key/algorithm
            SecretLog.CookieUnprotectFailed(_logger, e, name);
        }

        return rank;
    }

    private void ClearOverflow()
    {
        var names = GetCookieNames();
        var toKeep = _options.UserInteraction.CookieMessageThreshold;

        if (names.Count() >= toKeep)
        {
            var rankedCookieNames =
                from name in names
                let rank = GetCookieRank(name)
                orderby rank descending
                select name;

            var purge = rankedCookieNames.Skip(Math.Max(0, toKeep - 1));
            foreach (var name in purge)
            {
                SecretLog.PurgingStaleCookie(_logger, name);
                ClearByCookieName(name);
            }
        }
    }
}
