namespace MinApiLib.Oidc.Http.ClientCredentials;

/// <summary>
/// Keeps one token per client, in memory, and asks for a new one shortly before it expires. Concurrent callers share
/// the same token request.
/// </summary>
internal sealed partial class ClientCredentialsTokenManager(
    IOptionsMonitor<ClientCredentialsOptions> options,
    IHttpClientFactory httpClients,
    DiscoveryCache discovery,
    TokenEndpointClient tokenEndpoint,
    TimeProvider time,
    ILogger<ClientCredentialsTokenManager> logger) : IClientCredentialsTokenManager, IDisposable
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public async Task<AccessToken> GetAccessTokenAsync(string clientName, bool forceRenewal = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        var settings = options.Get(clientName);
        var entry = _entries.GetOrAdd(clientName, _ => new Entry());

        var seen = entry.Token;
        if (!forceRenewal && seen != null && seen.IsValid(time.GetUtcNow(), settings.RenewBeforeExpiration))
        {
            return seen;
        }

        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have got a new token while this one waited: a renewal asks for the token the caller
            // saw to be replaced, not for one more.
            var current = entry.Token;
            if (current != null && (!forceRenewal || !ReferenceEquals(current, seen)) && current.IsValid(time.GetUtcNow(), settings.RenewBeforeExpiration))
            {
                return current;
            }

            entry.Token = await RequestAsync(clientName, settings, entry, cancellationToken);
            return entry.Token;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private async Task<AccessToken> RequestAsync(string clientName, ClientCredentialsOptions settings, Entry entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settings.ClientId))
        {
            throw new InvalidOperationException($"The client credentials client '{clientName}' has no ClientId. Register it with AddClientCredentialsTokens(\"{clientName}\", ...).");
        }

        Uri endpoint;
        if (!string.IsNullOrEmpty(settings.TokenEndpoint))
        {
            endpoint = new Uri(settings.TokenEndpoint);
        }
        else if (!string.IsNullOrEmpty(settings.Authority))
        {
            endpoint = await discovery.GetTokenEndpointAsync(settings.Authority, settings.RequireHttpsMetadata, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"The client credentials client '{clientName}' needs an Authority or a TokenEndpoint.");
        }

        if (settings.UseDPoP)
        {
            entry.Key ??= settings.DPoPJsonWebKey != null ? DPoPKey.FromJwk(settings.DPoPJsonWebKey) : DPoPKey.CreateECDsa();
        }

        var parameters = new Dictionary<string, string>(settings.Parameters, StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = settings.ClientId
        };
        if (settings.ClientSecret != null)
        {
            parameters["client_secret"] = settings.ClientSecret;
        }

        if (!string.IsNullOrWhiteSpace(settings.Scope))
        {
            parameters["scope"] = settings.Scope;
        }

        var key = settings.UseDPoP ? entry.Key : null;
        var response = await tokenEndpoint.RequestAsync(httpClients.CreateClient(OidcHttpDefaults.HttpClientName), endpoint, parameters, key, cancellationToken);
        if (response.IsError)
        {
            throw response.ToException();
        }

        var tokenType = response.TokenType ?? AccessToken.BearerTokenType;
        LogTokenReceived(logger, clientName, tokenType);
        return new AccessToken(response.AccessToken!, tokenType, response.ExpiresAt(time.GetUtcNow()), key);
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
        {
            entry.Lock.Dispose();
            entry.Key?.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "New {TokenType} access token for the client credentials client {ClientName}")]
    private static partial void LogTokenReceived(ILogger logger, string clientName, string tokenType);

    private sealed class Entry
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public AccessToken? Token { get; set; }

        public DPoPKey? Key { get; set; }
    }
}
