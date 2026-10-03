namespace MinApiLib.Oidc.Http;

/// <summary>
/// The discovery documents of the authorities, read once and refreshed by <see cref="ConfigurationManager{T}"/>.
/// </summary>
internal sealed class DiscoveryCache(IHttpClientFactory httpClients)
{
    private readonly ConcurrentDictionary<(string Authority, bool RequireHttps), ConfigurationManager<OpenIdConnectConfiguration>> _managers = new();

    public async Task<Uri> GetTokenEndpointAsync(string authority, bool requireHttps, CancellationToken cancellationToken)
    {
        var manager = _managers.GetOrAdd((authority.TrimEnd('/'), requireHttps), key => new ConfigurationManager<OpenIdConnectConfiguration>(
            key.Authority + "/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(httpClients.CreateClient(OidcHttpDefaults.HttpClientName)) { RequireHttps = key.RequireHttps }));

        var configuration = await manager.GetConfigurationAsync(cancellationToken);
        return string.IsNullOrEmpty(configuration.TokenEndpoint)
            ? throw new AccessTokenException($"The discovery document of {authority} has no token endpoint.")
            : new Uri(configuration.TokenEndpoint);
    }
}
