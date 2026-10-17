namespace MinApiLib.Oidc.JwtBearer.Tests;

/// <summary>
/// The three sides together: MinApiLib.Oidc.Server issues the tokens, MinApiLib.Oidc.Http gets and sends them, and an
/// API with <c>AddJwtBearer().AddDPoP()</c> validates them.
/// </summary>
public sealed class JwtBearerDPoPTests : IAsyncDisposable
{
    private TestOidcServer? _server;
    private TestApi? _api;
    private ServiceProvider? _services;

    public async ValueTask DisposeAsync()
    {
        if (_services != null)
        {
            await _services.DisposeAsync();
        }

        if (_api != null)
        {
            await _api.DisposeAsync();
        }

        if (_server != null)
        {
            await _server.DisposeAsync();
        }
    }

    private async Task StartAsync(Action<DPoPJwtBearerOptions>? configure = null)
    {
        _server = await TestOidcServer.StartAsync();
        _api = await TestApi.StartAsync(_server, configure);
        var server = _server;
        var api = _api;

        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var (name, clientId, useDPoP) in new[] { ("bearer", "worker", false), ("dpop", "worker.dpop", true) })
        {
            services.AddClientCredentialsTokens(name, options =>
            {
                options.Authority = TestOidcServer.Url;
                options.ClientId = clientId;
                options.ClientSecret = TestOidcServer.Secret;
                options.Scope = TestOidcServer.ApiScope;
                options.UseDPoP = useDPoP;
            });
            services.AddHttpClient(name, client => client.BaseAddress = new Uri(TestApi.Url))
                .ConfigurePrimaryHttpMessageHandler(api.CreateHandler)
                .AddClientCredentialsTokenHandler(name);
        }

        services.AddHttpClient(OidcHttpDefaults.HttpClientName).ConfigurePrimaryHttpMessageHandler(server.CreateHandler);
        services.AddHttpClient("raw", client => client.BaseAddress = new Uri(TestApi.Url)).ConfigurePrimaryHttpMessageHandler(api.CreateHandler);
        _services = services.BuildServiceProvider();
    }

    private HttpClient Client(string name) => _services!.GetRequiredService<IHttpClientFactory>().CreateClient(name);

    private Task<AccessToken> TokenAsync(string name)
        => _services!.GetRequiredService<IClientCredentialsTokenManager>().GetAccessTokenAsync(name, cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static string[] Challenges(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return [.. response.Headers.WwwAuthenticate.Select(h => h.ToString())];
    }

    private async Task<HttpResponseMessage> SendAsync(string scheme, string token, string? proof, string url = TestApi.Url + "/orders")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"{scheme} {token}");
        if (proof != null)
        {
            request.Headers.Add(DPoPConstants.HeaderName, proof);
        }

        return await Client("raw").SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DPoP_bound_token_with_its_proof_is_accepted()
    {
        await StartAsync();

        var api = await OkAsync(await Client("dpop").GetAsync("/orders", TestContext.Current.CancellationToken));
        var token = await TokenAsync("dpop");

        Assert.Equal("worker.dpop", api.GetProperty("client_id").GetString());
        Assert.Equal("DPoP", api.GetProperty("scheme").GetString());
        Assert.Equal(token.DPoPKey!.Thumbprint, api.GetProperty("jkt").GetString());
    }

    [Fact]
    public async Task Bearer_token_is_accepted_when_DPoP_is_allowed()
    {
        await StartAsync();

        var api = await OkAsync(await Client("bearer").GetAsync("/orders", TestContext.Current.CancellationToken));

        Assert.Equal("Bearer", api.GetProperty("scheme").GetString());
        Assert.Equal(JsonValueKind.Null, api.GetProperty("jkt").ValueKind);
    }

    [Fact]
    public async Task Bearer_token_is_rejected_when_DPoP_is_required()
    {
        await StartAsync(options => options.Mode = DPoPTokenMode.Required);

        var challenges = Challenges(await Client("bearer").GetAsync("/orders", TestContext.Current.CancellationToken));

        var challenge = Assert.Single(challenges);
        Assert.StartsWith("DPoP algs=\"ES256 PS256\", error=\"invalid_token\"", challenge);
        Assert.Contains("Bearer tokens are not accepted", challenge);
    }

    [Fact]
    public async Task DPoP_bound_token_sent_as_a_bearer_token_is_rejected()
    {
        await StartAsync();
        var token = await TokenAsync("dpop");

        var challenges = Challenges(await SendAsync("Bearer", token.Value, proof: null));

        Assert.Contains("DPoP-bound access token sent without the DPoP scheme", challenges[0]);
        Assert.StartsWith("DPoP ", challenges[0]);
        Assert.Equal("Bearer realm=\"orders\"", challenges[1]);
    }

    [Fact]
    public async Task Proof_of_another_key_is_rejected()
    {
        await StartAsync();
        var token = await TokenAsync("dpop");
        using var otherKey = DPoPKey.CreateECDsa();

        var challenges = Challenges(await SendAsync("DPoP", token.Value, DPoPProof.Create(otherKey, HttpMethod.Get, new Uri(TestApi.Url + "/orders"), token.Value)));

        Assert.Contains("error=\"invalid_token\"", challenges[0]);
        Assert.Contains("does not match the key the access token is bound to", challenges[0]);
    }

    [Fact]
    public async Task Proof_is_accepted_once()
    {
        await StartAsync();
        var token = await TokenAsync("dpop");
        var proof = DPoPProof.Create(token.DPoPKey!, HttpMethod.Get, new Uri(TestApi.Url + "/orders"), token.Value);

        await OkAsync(await SendAsync("DPoP", token.Value, proof));
        var challenges = Challenges(await SendAsync("DPoP", token.Value, proof));

        Assert.Contains("error=\"invalid_dpop_proof\"", challenges[0]);
        Assert.Contains("already been used", challenges[0]);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("other-url")]
    [InlineData("no-ath")]
    public async Task Proof_that_does_not_fit_the_request_is_rejected(string fault)
    {
        await StartAsync();
        var token = await TokenAsync("dpop");
        var proof = fault switch
        {
            "missing" => null,
            "other-url" => DPoPProof.Create(token.DPoPKey!, HttpMethod.Get, new Uri(TestApi.Url + "/invoices"), token.Value),
            _ => DPoPProof.Create(token.DPoPKey!, HttpMethod.Get, new Uri(TestApi.Url + "/orders"))
        };

        var challenges = Challenges(await SendAsync("DPoP", token.Value, proof));

        Assert.Contains("error=\"invalid_dpop_proof\"", challenges[0]);
    }

    [Fact]
    public async Task Request_without_a_token_gets_both_challenges_without_an_error()
    {
        await StartAsync();

        var challenges = Challenges(await Client("raw").GetAsync("/orders", TestContext.Current.CancellationToken));

        Assert.Equal(["Bearer realm=\"orders\"", "DPoP algs=\"ES256 PS256\""], challenges);
    }

    [Fact]
    public async Task Required_nonce_is_sent_by_the_API_and_used_by_the_client_handler()
    {
        await StartAsync(options => options.RequireNonce = true);

        // The handler gets use_dpop_nonce with a DPoP-Nonce, and sends the request again with a proof that has it.
        var api = await OkAsync(await Client("dpop").PostAsync("/orders", new StringContent("{}"), TestContext.Current.CancellationToken));

        Assert.Equal("DPoP", api.GetProperty("scheme").GetString());
        var calls = _api!.Log.To("/orders");
        Assert.Equal(2, calls.Count);
        Assert.False(new JsonWebToken(calls[0].DPoP).TryGetPayloadValue<string>("nonce", out _));
        Assert.True(new JsonWebToken(calls[1].DPoP).TryGetPayloadValue<string>("nonce", out var nonce) && nonce.Length > 0);
    }

    [Fact]
    public async Task Nonce_the_API_did_not_issue_is_rejected_with_a_new_one()
    {
        await StartAsync(options => options.RequireNonce = true);
        var token = await TokenAsync("dpop");

        var response = await SendAsync("DPoP", token.Value, DPoPProof.Create(token.DPoPKey!, HttpMethod.Get, new Uri(TestApi.Url + "/orders"), token.Value, nonce: "made-up"));

        Assert.Contains("error=\"use_dpop_nonce\"", Challenges(response)[0]);
        Assert.True(response.Headers.TryGetValues(DPoPConstants.NonceHeaderName, out var nonces) && nonces.Single().Length > 20);
    }
}
