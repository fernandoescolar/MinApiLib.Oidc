namespace MinApiLib.OidcServer.Client.OpenIdConnect;

/// <summary>
/// Reads the tokens that the OpenID Connect handler saved in the session, and refreshes them with the scheme that
/// signed the user in. Within a request, the session is read once; across requests, concurrent refreshes of the same
/// refresh token share one token request, so a one-time refresh token is not used twice.
/// </summary>
internal sealed partial class UserAccessTokenManager(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    IOptionsMonitor<OpenIdConnectTokenOptions> tokenOptions,
    TokenEndpointClient tokenEndpoint,
    TimeProvider time,
    ILogger<UserAccessTokenManager> logger) : IUserAccessTokenManager
{
    private const string ItemPrefix = "MinApiLib.OidcServer.Client.UserSession:";
    private static readonly TimeSpan SharedRefreshLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultRefreshBeforeExpiration = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, SharedRefresh> _refreshes = new(StringComparer.Ordinal);

    public async Task<AccessToken?> GetAccessTokenAsync(HttpContext httpContext, string? signInScheme = null, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var itemKey = ItemPrefix + signInScheme;
        var session = httpContext.Items[itemKey] as UserSession ?? await ReadAsync(httpContext, signInScheme);
        if (session == null)
        {
            return null;
        }

        httpContext.Items[itemKey] = session;

        var margin = session.Scheme == null ? DefaultRefreshBeforeExpiration : tokenOptions.Get(session.Scheme).RefreshBeforeExpiration;
        var expiring = !session.Token.IsValid(time.GetUtcNow(), margin);

        // Without a refresh token, or a scheme to refresh with, the token goes as it is: the API decides. A token
        // refreshed in this request is not refreshed again.
        if ((!expiring && !forceRefresh) || session.RefreshToken == null || session.Scheme == null || session.Refreshed)
        {
            return session.Token;
        }

        session = await RefreshAsync(httpContext, session, cancellationToken);
        httpContext.Items[itemKey] = session;
        return session.Token;
    }

    private static async Task<UserSession?> ReadAsync(HttpContext httpContext, string? signInScheme)
    {
        var result = await httpContext.AuthenticateAsync(signInScheme);
        if (!result.Succeeded || result.Ticket == null)
        {
            return null;
        }

        var properties = result.Ticket.Properties;
        var value = properties.GetTokenValue("access_token");
        if (value == null)
        {
            return null;
        }

        DPoPKey? key = null;
        if (properties.Items.TryGetValue(OidcClientDefaults.DPoPKeyProperty, out var jwk) && jwk != null)
        {
            key = DPoPKey.FromJwk(jwk);
            httpContext.Response.RegisterForDispose(key);
        }

        var token = new AccessToken(value, properties.GetTokenValue("token_type") ?? AccessToken.BearerTokenType, ParseExpiresAt(properties.GetTokenValue("expires_at")), key);
        return new UserSession(result.Ticket, token, properties.GetTokenValue("refresh_token"), properties.Items.TryGetValue(OidcClientDefaults.SchemeProperty, out var scheme) ? scheme : null, Refreshed: false);
    }

    private async Task<UserSession> RefreshAsync(HttpContext httpContext, UserSession session, CancellationToken cancellationToken)
    {
        var refreshToken = session.RefreshToken!;
        var oidc = oidcOptions.Get(session.Scheme);
        var configuration = oidc.Configuration ?? await oidc.ConfigurationManager!.GetConfigurationAsync(cancellationToken);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = oidc.ClientId!
        };
        if (!string.IsNullOrEmpty(oidc.ClientSecret))
        {
            parameters["client_secret"] = oidc.ClientSecret;
        }

        // A token bound to a key is refreshed with a proof of the same key (RFC 9449 section 5).
        var keyJwk = session.Token.IsDPoP ? session.Token.DPoPKey!.ToJwk() : null;

        var refresh = Share(refreshToken, () => RequestAsync(oidc.Backchannel, new Uri(configuration.TokenEndpoint), parameters, keyJwk));
        var response = await refresh.WaitAsync(cancellationToken);
        if (response.IsError)
        {
            _refreshes.TryRemove(RefreshKey(refreshToken), out _);
            throw response.ToException();
        }

        var now = time.GetUtcNow();
        var tokens = session.Ticket.Properties.GetTokens().ToDictionary(t => t.Name, t => t.Value, StringComparer.Ordinal);
        tokens["access_token"] = response.AccessToken!;
        tokens["token_type"] = response.TokenType ?? AccessToken.BearerTokenType;
        if (response.RefreshToken != null)
        {
            tokens["refresh_token"] = response.RefreshToken;
        }

        var expiresAt = response.ExpiresAt(now);
        if (expiresAt != null)
        {
            tokens["expires_at"] = expiresAt.Value.ToString("o", CultureInfo.InvariantCulture);
        }
        else
        {
            tokens.Remove("expires_at");
        }

        var properties = session.Ticket.Properties;
        properties.StoreTokens(tokens.Select(t => new AuthenticationToken { Name = t.Key, Value = t.Value }));

        if (httpContext.Response.HasStarted)
        {
            LogCannotUpdateSession(logger, session.Scheme!);
        }
        else
        {
            await httpContext.SignInAsync(session.Ticket.AuthenticationScheme, session.Ticket.Principal, properties);
        }

        LogRefreshed(logger, session.Scheme!);
        var token = new AccessToken(response.AccessToken!, tokens["token_type"], expiresAt, session.Token.DPoPKey);
        return session with { Token = token, RefreshToken = tokens.GetValueOrDefault("refresh_token"), Refreshed = true };
    }

    private async Task<TokenEndpointResponse> RequestAsync(HttpClient backchannel, Uri endpoint, Dictionary<string, string> parameters, string? keyJwk)
    {
        // The shared request has its own copy of the key: the request that started it may end first.
        using var key = keyJwk == null ? null : DPoPKey.FromJwk(keyJwk);
        return await tokenEndpoint.RequestAsync(backchannel, endpoint, parameters, key, CancellationToken.None);
    }

    private Task<TokenEndpointResponse> Share(string refreshToken, Func<Task<TokenEndpointResponse>> request)
    {
        var now = time.GetUtcNow();
        foreach (var (key, old) in _refreshes)
        {
            if (now - old.Started > SharedRefreshLifetime)
            {
                _refreshes.TryRemove(key, out _);
            }
        }

        return _refreshes.GetOrAdd(RefreshKey(refreshToken), _ => new SharedRefresh(now, new Lazy<Task<TokenEndpointResponse>>(request))).Response.Value;
    }

    private static string RefreshKey(string refreshToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static DateTimeOffset? ParseExpiresAt(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt) ? expiresAt : null;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshed the user's access token with the scheme {Scheme}")]
    private static partial void LogRefreshed(ILogger logger, string scheme);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refreshed the user's access token with the scheme {Scheme} after the response started: the session cookie keeps the old tokens. Call the API before writing the response")]
    private static partial void LogCannotUpdateSession(ILogger logger, string scheme);

    private sealed record UserSession(AuthenticationTicket Ticket, AccessToken Token, string? RefreshToken, string? Scheme, bool Refreshed);

    private sealed record SharedRefresh(DateTimeOffset Started, Lazy<Task<TokenEndpointResponse>> Response);
}
