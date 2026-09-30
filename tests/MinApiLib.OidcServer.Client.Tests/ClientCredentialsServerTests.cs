namespace MinApiLib.OidcServer.Client.Tests;

/// <summary>
/// Client credentials against MinApiLib.OidcServer: discovery, the token endpoint and an API that validates the
/// token and, for DPoP, the proof.
/// </summary>
public sealed class ClientCredentialsServerTests : IAsyncDisposable
{
    private TestOidcServer? _server;
    private ServiceProvider? _services;

    public async ValueTask DisposeAsync()
    {
        if (_services != null)
        {
            await _services.DisposeAsync();
        }

        if (_server != null)
        {
            await _server.DisposeAsync();
        }
    }

    private async Task<(HttpClient Api, IClientCredentialsTokenManager Tokens)> CreateAsync(string clientId, bool useDPoP)
    {
        _server = await TestOidcServer.StartAsync();
        var server = _server;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddClientCredentialsTokens("worker", options =>
        {
            options.Authority = TestOidcServer.Url;
            options.ClientId = clientId;
            options.ClientSecret = TestOidcServer.Secret;
            options.Scope = TestOidcServer.ApiScope;
            options.UseDPoP = useDPoP;
        });
        services.AddHttpClient(OidcClientDefaults.HttpClientName).ConfigurePrimaryHttpMessageHandler(server.CreateHandler);
        services.AddHttpClient("api", client => client.BaseAddress = new Uri(TestOidcServer.Url))
            .ConfigurePrimaryHttpMessageHandler(server.CreateHandler)
            .AddClientCredentialsTokenHandler("worker");

        _services = services.BuildServiceProvider();
        return (_services.GetRequiredService<IHttpClientFactory>().CreateClient("api"), _services.GetRequiredService<IClientCredentialsTokenManager>());
    }

    private static async Task<JsonElement> WhoAmIAsync(HttpClient api)
    {
        var response = await api.GetAsync("/api/whoami", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bearer_token_from_discovery_calls_the_API()
    {
        var (api, _) = await CreateAsync("worker", useDPoP: false);

        var first = await WhoAmIAsync(api);
        await WhoAmIAsync(api);

        Assert.Equal("worker", first.GetProperty("client_id").GetString());
        Assert.Equal("Bearer", first.GetProperty("scheme").GetString());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("jkt").ValueKind);
        Assert.Single(_server!.Log.To("/.well-known/openid-configuration"));
        Assert.Single(_server.Log.To("/connect/token"));
    }

    [Fact]
    public async Task DPoP_token_is_bound_to_the_client_key_and_every_call_has_a_new_proof()
    {
        var (api, tokens) = await CreateAsync("worker.dpop", useDPoP: true);

        var first = await WhoAmIAsync(api);
        await WhoAmIAsync(api);

        var token = await tokens.GetAccessTokenAsync("worker", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("DPoP", token.TokenType);
        Assert.Equal("DPoP", first.GetProperty("scheme").GetString());
        Assert.Equal(token.DPoPKey!.Thumbprint, first.GetProperty("jkt").GetString());

        Assert.NotNull(Assert.Single(_server!.Log.To("/connect/token")).DPoP);
        var proofs = _server.Log.To("/api/whoami").Select(r => new JsonWebToken(r.DPoP).Id).ToList();
        Assert.Equal(2, proofs.Distinct().Count());
    }

    [Fact]
    public async Task Client_that_requires_DPoP_is_refused_without_it()
    {
        var (api, _) = await CreateAsync("worker.dpop", useDPoP: false);

        var ex = await Assert.ThrowsAsync<AccessTokenException>(() => api.GetAsync("/api/whoami", TestContext.Current.CancellationToken));

        Assert.Equal("invalid_request", ex.Error);
    }
}
