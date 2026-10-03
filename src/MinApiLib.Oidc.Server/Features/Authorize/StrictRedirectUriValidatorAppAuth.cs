using System.Text.RegularExpressions;

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Strict redirect URI validator that also accepts loopback redirect URIs for native apps (RFC 8252 section 7.3):
/// a loopback URI may use any port, while scheme, host, path and query must match a registered loopback URI.
/// </summary>
/// <remarks>
/// Supported loopback hosts are <c>127.0.0.1</c> and <c>[::1]</c>, always with the <c>http</c> scheme and a port
/// between 1 and 65535. A registration of exactly <c>http://127.0.0.1</c> or <c>http://[::1]</c> (no port, path or
/// query) accepts any path and query on that loopback address, for backwards compatibility. Fragments are never
/// accepted (RFC 6749 section 3.1.2). Loopback URIs are only accepted for clients that require PKCE.
/// </remarks>
/// <seealso cref="MinApiLib.Oidc.Server.Features.Authorize.StrictRedirectUriValidator" />
public partial class StrictRedirectUriValidatorAppAuth : StrictRedirectUriValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictRedirectUriValidatorAppAuth"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public StrictRedirectUriValidatorAppAuth(ILogger<StrictRedirectUriValidatorAppAuth> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines whether a redirect URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    /// <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public override async Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
    {
        var isAllowed = await base.IsRedirectUriValidAsync(requestedUri, client);
        if (isAllowed) return isAllowed;

        return client.RequirePkce && IsRegisteredLoopback(requestedUri, client.RedirectUris);
    }

    /// <summary>
    /// Determines whether a post logout URI is valid for a client.
    /// </summary>
    /// <param name="requestedUri">The requested URI.</param>
    /// <param name="client">The client.</param>
    /// <returns>
    /// <c>true</c> is the URI is valid; <c>false</c> otherwise.
    /// </returns>
    public override async Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
    {
        var isAllowed = await base.IsPostLogoutRedirectUriValidAsync(requestedUri, client);
        if (isAllowed) return isAllowed;

        return client.RequirePkce && IsRegisteredLoopback(requestedUri, client.PostLogoutRedirectUris);
    }

    private bool IsRegisteredLoopback(string requestedUri, IEnumerable<string> registeredUris)
    {
        if (!TryParseLoopback(requestedUri, out var host, out var port, out var pathAndQuery) || port is null or < 1 or > 65535)
        {
            RequestValidationLog.InvalidLoopbackRedirectUri(_logger, requestedUri);
            return false;
        }

        foreach (var registered in registeredUris ?? [])
        {
            if (!TryParseLoopback(registered, out var registeredHost, out var registeredPort, out var registeredPathAndQuery) ||
                registeredHost != host)
            {
                continue;
            }

            var isLegacyAnyPath = registeredPort is null && registeredPathAndQuery.Length == 0;
            if (isLegacyAnyPath || string.Equals(registeredPathAndQuery, pathAndQuery, StringComparison.Ordinal))
            {
                return true;
            }
        }

        RequestValidationLog.UnregisteredLoopbackRedirectUri(_logger, requestedUri);
        return false;
    }

    private static bool TryParseLoopback(string uri, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? host, out int? port, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? pathAndQuery)
    {
        host = null;
        port = null;
        pathAndQuery = null;

        if (uri == null) return false;

        var match = LoopbackRegex().Match(uri);
        if (!match.Success) return false;

        host = match.Groups["host"].Value;
        pathAndQuery = match.Groups["rest"].Value;

        if (match.Groups["port"].Success)
        {
            if (!int.TryParse(match.Groups["port"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) return false;
            port = value;
        }

        return true;
    }

    [GeneratedRegex(@"^http://(?<host>127\.0\.0\.1|\[::1\])(?::(?<port>[0-9]{1,5}))?(?<rest>[/?][^#]*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex LoopbackRegex();
}
