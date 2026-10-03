namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Default implementation of <see cref="IJwtRequestUriHttpClient"/> that fetches a JWT
/// from a <c>request_uri</c> parameter during the authorize endpoint flow.
/// Enforces HTTPS and rejects requests targeting private or reserved IP ranges to prevent SSRF.
/// </summary>
public class DefaultJwtRequestUriHttpClient : IJwtRequestUriHttpClient
{
    private readonly HttpClient _client;
    private readonly OidcServerOptions _options;
    private readonly ILogger<DefaultJwtRequestUriHttpClient> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultJwtRequestUriHttpClient"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used to fetch the remote JWT.</param>
    /// <param name="options">The <see cref="OidcServerOptions"/> controlling JAR validation behaviour.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to create the logger.</param>
    public DefaultJwtRequestUriHttpClient(HttpClient client, OidcServerOptions options, ILoggerFactory loggerFactory)
    {
        _client = client;
        _options = options;
        _logger = loggerFactory.CreateLogger<DefaultJwtRequestUriHttpClient>();
    }

    /// <inheritdoc />
    public async Task<string?> GetJwtAsync(string url, Client client, CancellationToken cancellationToken = default)
    {
        if (!IsValidRequestUri(url))
        {
            _logger.JwtRequestUriInvalidUrl(url);
            return null;
        }

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Options.Set(new HttpRequestOptionsKey<Client>(OidcServerConstants.JwtRequestClientKey), client);

        HttpResponseMessage response;
        try
        {
            response = await _client.SendAsync(req, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.JwtRequestUriFetchFailed(ex, url);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.JwtRequestUriFetchFailed(ex, url);
            return null;
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {
            if (_options.StrictJarValidation)
            {
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (!string.Equals(mediaType,
                        $"application/{JwtClaimTypes.JwtTypes.AuthorizationRequest}", StringComparison.Ordinal))
                {
                    _logger.JwtRequestUriInvalidContentType(mediaType ?? "(none)", url);
                    return null;
                }
            }

            var maxSize = _options.InputLengthRestrictions.Jwt;
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxSize)
            {
                _logger.JwtRequestUriContentLengthExceeded(url, contentLength.Value, maxSize);
                return null;
            }

            _logger.JwtRequestUriSuccess(url);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (json.Length > maxSize)
            {
                _logger.JwtRequestUriBodySizeExceeded(url, json.Length, maxSize);
                return null;
            }

            return json;
        }

        _logger.JwtRequestUriInvalidStatusCode(response.StatusCode, url);
        return null;
    }

    /// <remarks>
    /// IP address literals are rejected here early. Host names are resolved and checked at connect time by
    /// the handler from <see cref="JwtRequestUriHttpMessageHandler"/>, which also refuses redirects.
    /// </remarks>
    private static bool IsValidRequestUri(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (IPAddress.TryParse(uri.Host, out var ip) && JwtRequestUriHttpMessageHandler.IsPrivateOrReservedIp(ip))
            return false;

        return true;
    }
}
