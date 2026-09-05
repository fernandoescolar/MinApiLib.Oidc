namespace IdentityServer.Services;

/// <summary>
/// Default issuer resolution: <see cref="IdentityServerOptions.IssuerUri"/> when configured, otherwise the origin
/// and base path of the current request. With <c>IssuerUri</c> configured no HTTP request is needed, so tokens can
/// also be issued from background work.
/// </summary>
public class DefaultIssuerNameService : IIssuerNameService
{
    private readonly IdentityServerOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultIssuerNameService"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public DefaultIssuerNameService(IdentityServerOptions options, IHttpContextAccessor httpContextAccessor)
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
            ?? throw new InvalidOperationException("The issuer cannot be derived outside an HTTP request. Configure IdentityServerOptions.IssuerUri.");

        return Task.FromResult(context.GetIdentityServerIssuerUri());
    }
}
