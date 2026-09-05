namespace MinApiLib.OidcServer.Endpoints.Results;

/// <summary>
/// Result for endsession
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Hosting.IEndpointResult" />
public class EndSessionResult : IEndpointResult
{
    private readonly EndSessionValidationResult _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndSessionResult"/> class.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <exception cref="System.ArgumentNullException">result</exception>
    public EndSessionResult(EndSessionValidationResult result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
    }

    internal EndSessionResult(
        EndSessionValidationResult result,
        OidcServerOptions options,
        TimeProvider clock,
        IMessageStore<LogoutMessage> logoutMessageStore)
        : this(result)
    {
        _options = options;
        _clock = clock;
        _logoutMessageStore = logoutMessageStore;
    }

    private OidcServerOptions _options = null!;
    private TimeProvider _clock = null!;
    private IMessageStore<LogoutMessage> _logoutMessageStore = null!;

    private void Init(HttpContext context)
    {
        _options = _options ?? context.RequestServices.GetRequiredService<OidcServerOptions>();
        _clock = _clock ?? context.RequestServices.GetRequiredService<TimeProvider>();
        _logoutMessageStore = _logoutMessageStore ?? context.RequestServices.GetRequiredService<IMessageStore<LogoutMessage>>();
    }

    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public async Task ExecuteAsync(HttpContext context)
    {
        Init(context);

        var validatedRequest = _result.IsError ? null : _result.ValidatedRequest;

        string? id = null;

        if (validatedRequest != null)
        {
            var logoutMessage = new LogoutMessage(validatedRequest);
            if (logoutMessage.ContainsPayload)
            {
                var msg = new Message<LogoutMessage>(logoutMessage, _clock.GetUtcNow().UtcDateTime);
                id = await _logoutMessageStore.WriteAsync(msg);
            }
        }

        var redirect = _options.UserInteraction.LogoutUrl
            ?? throw new InvalidOperationException("UserInteraction.LogoutUrl is not configured.");

        if (redirect.IsLocalUrl())
        {
            redirect = context.GetOidcServerRelativeUrl(redirect) ?? redirect;
        }

        if (id != null)
        {
            redirect = redirect.AddQueryString(_options.UserInteraction.LogoutIdParameter, id);
        }

        context.Response.RedirectSeeOtherAfterPost(redirect);
    }
}
