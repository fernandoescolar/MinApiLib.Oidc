namespace MinApiLib.OidcServer.Services;

/// <summary>
/// Default issuer resolution: <see cref="OidcServerOptions.IssuerUri"/> when configured, otherwise the origin
/// and base path of the current request. With <c>IssuerUri</c> configured no HTTP request is needed, so tokens can
/// also be issued from background work.
/// </summary>
[RegisterInIServiceCollection(typeof(IIssuerNameService))]
public class DefaultIssuerNameService : IIssuerNameService
{
    private readonly OidcServerOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultIssuerNameService"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public DefaultIssuerNameService(OidcServerOptions options, IHttpContextAccessor httpContextAccessor)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public virtual Task<string> GetCurrentAsync()
    {
        if (_options.IssuerUri.IsPresent())
        {
            return Task.FromResult(_options.IssuerUri);
        }

        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("The issuer cannot be derived outside an HTTP request. Configure OidcServerOptions.IssuerUri.");

        return Task.FromResult(context.GetOidcServerIssuerUri());
    }
}
