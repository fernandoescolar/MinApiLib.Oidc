namespace MinApiLib.OidcServer.Client.Tests;

/// <summary>
/// The client credentials handler against a stub token endpoint and a stub API: caching, renewal, nonces.
/// </summary>
public sealed class ClientCredentialsHandlerTests : IDisposable
{
    private const string TokenEndpoint = "https://as.example/token";
    private ServiceProvider? _services;

    public void Dispose() => _services?.Dispose();

    private (HttpClient Api, IClientCredentialsTokenManager Tokens) Create(StubHandler tokenEndpoint, StubHandler api, Action<ClientCredentialsOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddClientCredentialsTokens("worker", options =>
        {
            options.TokenEndpoint = TokenEndpoint;
            options.ClientId = "worker";
            options.ClientSecret = "secret";
            options.Scope = "orders.read";
            configure?.Invoke(options);
        });
        services.AddHttpClient(OidcClientDefaults.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Handlers.NonDisposing(tokenEndpoint));
        services.AddHttpClient("api", client => client.BaseAddress = new Uri("https://api.example"))
            .ConfigurePrimaryHttpMessageHandler(() => Handlers.NonDisposing(api))
            .AddClientCredentialsTokenHandler("worker");

        _services = services.BuildServiceProvider();
        return (_services.GetRequiredService<IHttpClientFactory>().CreateClient("api"), _services.GetRequiredService<IClientCredentialsTokenManager>());
    }

    private static StubHandler Tokens(string tokenType = "Bearer", int expiresIn = 3600)
        => new((_, i) => StubHandler.Json(HttpStatusCode.OK, new { access_token = $"token-{i}", token_type = tokenType, expires_in = expiresIn }));

    private static StubHandler Ok() => new((_, _) => new HttpResponseMessage(HttpStatusCode.OK));

    [Fact]
    public async Task Sends_the_bearer_token_and_reuses_it()
    {
        var tokenEndpoint = Tokens();
        var api = Ok();
        var (http, _) = Create(tokenEndpoint, api);

        await http.GetAsync("/orders", TestContext.Current.CancellationToken);
        await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        var request = Assert.Single(tokenEndpoint.Log.Requests);
        Assert.Equal("client_credentials", request.Form("grant_type"));
        Assert.Equal("worker", request.Form("client_id"));
        Assert.Equal("secret", request.Form("client_secret"));
        Assert.Equal("orders.read", request.Form("scope"));
        Assert.Null(request.DPoP);
        Assert.All(api.Log.Requests, r =>
        {
            Assert.Equal("Bearer token-0", r.Authorization);
            Assert.Null(r.DPoP);
        });
    }

    [Fact]
    public async Task Concurrent_requests_share_one_token_request()
    {
        var tokenEndpoint = new StubHandler((_, i) => StubHandler.Json(HttpStatusCode.OK, new { access_token = $"token-{i}", token_type = "Bearer", expires_in = 3600 }))
        {
            Delay = TimeSpan.FromMilliseconds(100)
        };
        var (http, _) = Create(tokenEndpoint, Ok());

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => http.GetAsync("/orders", TestContext.Current.CancellationToken)));

        Assert.Single(tokenEndpoint.Log.Requests);
    }

    [Fact]
    public async Task Token_about_to_expire_is_replaced()
    {
        // 30 seconds of life is less than the default margin of 1 minute.
        var tokenEndpoint = Tokens(expiresIn: 30);
        var api = Ok();
        var (http, _) = Create(tokenEndpoint, api);

        await http.GetAsync("/orders", TestContext.Current.CancellationToken);
        await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        Assert.Equal(2, tokenEndpoint.Log.Requests.Count);
        Assert.Equal(["Bearer token-0", "Bearer token-1"], api.Log.Requests.Select(r => r.Authorization));
    }

    [Fact]
    public async Task Rejected_token_is_renewed_once()
    {
        var tokenEndpoint = Tokens();
        var api = new StubHandler((r, _) => new HttpResponseMessage(r.Authorization == "Bearer token-0" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
        var (http, _) = Create(tokenEndpoint, api);

        var response = await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Bearer token-0", "Bearer token-1"], api.Log.Requests.Select(r => r.Authorization));
    }

    [Fact]
    public async Task Unauthorized_twice_returns_the_second_answer()
    {
        var tokenEndpoint = Tokens();
        var api = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var (http, _) = Create(tokenEndpoint, api);

        var response = await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(2, api.Log.Requests.Count);
        Assert.Equal(2, tokenEndpoint.Log.Requests.Count);
    }

    [Fact]
    public async Task DPoP_token_is_sent_with_a_proof_of_the_client_key()
    {
        var tokenEndpoint = Tokens("DPoP");
        var api = Ok();
        var (http, tokens) = Create(tokenEndpoint, api, options => options.UseDPoP = true);

        await http.PostAsync("/orders?page=1", new StringContent("{}"), TestContext.Current.CancellationToken);

        var token = await tokens.GetAccessTokenAsync("worker", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(token.IsDPoP);

        var tokenRequestProof = new JsonWebToken(Assert.Single(tokenEndpoint.Log.Requests).DPoP);
        Assert.Equal("POST", tokenRequestProof.GetPayloadValue<string>("htm"));
        Assert.Equal(TokenEndpoint, tokenRequestProof.GetPayloadValue<string>("htu"));

        var call = Assert.Single(api.Log.Requests);
        Assert.Equal("DPoP token-0", call.Authorization);
        var proof = new JsonWebToken(call.DPoP);
        Assert.Equal("POST", proof.GetPayloadValue<string>("htm"));
        Assert.Equal("https://api.example/orders", proof.GetPayloadValue<string>("htu"));
        Assert.Equal(DPoPProof.HashAccessToken("token-0"), proof.GetPayloadValue<string>("ath"));
        Assert.True((await DPoPKeyTests.ValidateAsync(call.DPoP!, token.DPoPKey!.SecurityKey)).IsValid);
    }

    [Fact]
    public async Task Configured_DPoP_key_is_used()
    {
        using var key = DPoPKey.CreateRsa();
        var (http, tokens) = Create(Tokens("DPoP"), Ok(), options =>
        {
            options.UseDPoP = true;
            options.DPoPJsonWebKey = key.ToJwk();
        });

        var token = await tokens.GetAccessTokenAsync("worker", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(key.Thumbprint, token.DPoPKey!.Thumbprint);
        Assert.Equal("PS256", token.DPoPKey.Algorithm);
    }

    [Fact]
    public async Task Server_answering_Bearer_to_a_DPoP_request_gets_bearer_calls()
    {
        var api = Ok();
        var (http, _) = Create(Tokens("Bearer"), api, options => options.UseDPoP = true);

        await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        var call = Assert.Single(api.Log.Requests);
        Assert.Equal("Bearer token-0", call.Authorization);
        Assert.Null(call.DPoP);
    }

    [Fact]
    public async Task Token_endpoint_nonce_is_sent_again_in_the_proof()
    {
        var tokenEndpoint = new StubHandler((_, i) => i == 0
            ? StubHandler.Json(HttpStatusCode.BadRequest, new { error = "use_dpop_nonce", error_description = "Use the nonce" }, ("DPoP-Nonce", "as-nonce"))
            : StubHandler.Json(HttpStatusCode.OK, new { access_token = "token", token_type = "DPoP", expires_in = 3600 }));
        var (http, _) = Create(tokenEndpoint, Ok(), options => options.UseDPoP = true);

        var response = await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, tokenEndpoint.Log.Requests.Count);
        Assert.False(new JsonWebToken(tokenEndpoint.Log.Requests[0].DPoP).TryGetPayloadValue<string>("nonce", out _));
        Assert.Equal("as-nonce", new JsonWebToken(tokenEndpoint.Log.Requests[1].DPoP).GetPayloadValue<string>("nonce"));
    }

    [Fact]
    public async Task Resource_server_nonce_is_sent_again_in_the_proof_and_kept()
    {
        var api = new StubHandler((r, i) =>
        {
            if (i == 0)
            {
                var challenge = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                challenge.Headers.TryAddWithoutValidation("WWW-Authenticate", "DPoP error=\"use_dpop_nonce\", error_description=\"Resource server requires nonce in DPoP proof\"");
                challenge.Headers.TryAddWithoutValidation("DPoP-Nonce", "rs-nonce");
                return challenge;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var tokenEndpoint = Tokens("DPoP");
        var (http, _) = Create(tokenEndpoint, api, options => options.UseDPoP = true);

        var first = await http.GetAsync("/orders", TestContext.Current.CancellationToken);
        await http.GetAsync("/orders", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Single(tokenEndpoint.Log.Requests);
        var nonces = api.Log.Requests.Select(r => new JsonWebToken(r.DPoP).TryGetPayloadValue<string>("nonce", out var n) ? n : null).ToList();
        Assert.Equal([null, "rs-nonce", "rs-nonce"], nonces);
    }

    [Fact]
    public async Task Token_endpoint_error_is_thrown()
    {
        var tokenEndpoint = new StubHandler((_, _) => StubHandler.Json(HttpStatusCode.BadRequest, new { error = "invalid_client", error_description = "Unknown client" }));
        var (http, _) = Create(tokenEndpoint, Ok());

        var ex = await Assert.ThrowsAsync<AccessTokenException>(() => http.GetAsync("/orders", TestContext.Current.CancellationToken));

        Assert.Equal("invalid_client", ex.Error);
        Assert.Equal("Unknown client", ex.ErrorDescription);
    }

    [Fact]
    public async Task Client_without_a_server_fails_with_a_clear_error()
    {
        var (http, _) = Create(Tokens(), Ok(), options => options.TokenEndpoint = null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => http.GetAsync("/orders", TestContext.Current.CancellationToken));

        Assert.Contains("Authority or a TokenEndpoint", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Forced_renewal_gets_a_new_token()
    {
        var (_, tokens) = Create(Tokens(), Ok());
        var ct = TestContext.Current.CancellationToken;

        var first = await tokens.GetAccessTokenAsync("worker", cancellationToken: ct);
        var cached = await tokens.GetAccessTokenAsync("worker", cancellationToken: ct);
        var renewed = await tokens.GetAccessTokenAsync("worker", forceRenewal: true, cancellationToken: ct);

        Assert.Same(first, cached);
        Assert.Equal("token-1", renewed.Value);
    }
}
