namespace MinApiLib.OidcServer.IntegrationTests.Endpoints.Token;

/// <summary>
/// DPoP (RFC 9449) at the token, pushed authorization and userinfo endpoints.
/// </summary>
public class DPoPTests : IDisposable
{
    private const string Category = "DPoP";
    private const string TokenUrl = OidcServerPipeline.TokenEndpoint;
    private const string ParUrl = OidcServerPipeline.BaseUrl + "/connect/par";
    private const string RedirectUri = "https://client/callback";

    private readonly OidcServerPipeline _pipeline = new();
    private readonly DPoPProof _key = new();
    private readonly DPoPProof _otherKey = new();

    public DPoPTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "cc",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" }
        });
        _pipeline.Clients.Add(new Client
        {
            ClientId = "cc_required",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" },
            RequireDPoP = true
        });
        foreach (var (clientId, isPublic) in new[] { ("spa", true), ("web", false) })
        {
            _pipeline.Clients.Add(new Client
            {
                ClientId = clientId,
                RequireClientSecret = !isPublic,
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = false,
                RequirePkce = false,
                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "api" },
                RedirectUris = { RedirectUri }
            });
        }

        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = { new Claim("name", "Bob Loblaw") }
        });
        _pipeline.IdentityScopes.AddRange([new IdentityResources.OpenId(), new IdentityResources.Profile()]);
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _key.Dispose();
        _otherKey.Dispose();
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string Jkt(string accessToken)
    {
        var cnf = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims.SingleOrDefault(c => c.Type == "cnf")?.Value;
        return cnf == null ? null : JsonNode.Parse(cnf)["jkt"]?.ToString();
    }

    private async Task<(HttpStatusCode Status, JsonObject Json)> PostFormAsync(string url, Dictionary<string, string> form, params string[] proofs)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        foreach (var proof in proofs)
        {
            request.Headers.Add("DPoP", proof);
        }

        var response = await _pipeline.BackChannelClient.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response.StatusCode, body.Length == 0 ? new JsonObject() : JsonNode.Parse(body).AsObject());
    }

    private Task<(HttpStatusCode Status, JsonObject Json)> ClientCredentialsAsync(string clientId = "cc", params string[] proofs)
        => PostFormAsync(TokenUrl, new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", clientId },
            { "client_secret", "secret" },
            { "scope", "api" }
        }, proofs);

    private async Task<string> AuthorizeAsync(string clientId, string dpopJkt = null)
    {
        await _pipeline.LoginAsync("bob");
        var extra = new Parameters();
        if (dpopJkt != null)
        {
            extra.Add("dpop_jkt", dpopJkt);
        }

        var response = await _pipeline.RequestAuthorizationEndpointAsync(clientId, "code", "openid profile api offline_access", RedirectUri, state: "state", nonce: "nonce", extra: extra);
        return response.Code;
    }

    private Task<(HttpStatusCode Status, JsonObject Json)> RedeemAsync(string clientId, string code, string proof)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", clientId },
            { "code", code },
            { "redirect_uri", RedirectUri }
        };
        if (clientId == "web")
        {
            form.Add("client_secret", "secret");
        }

        return proof == null ? PostFormAsync(TokenUrl, form) : PostFormAsync(TokenUrl, form, proof);
    }

    private Task<(HttpStatusCode Status, JsonObject Json)> RefreshAsync(string clientId, string refreshToken, string proof)
    {
        var form = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", clientId },
            { "refresh_token", refreshToken }
        };
        if (clientId == "web")
        {
            form.Add("client_secret", "secret");
        }

        return proof == null ? PostFormAsync(TokenUrl, form) : PostFormAsync(TokenUrl, form, proof);
    }

    private async Task<HttpStatusCode> UserInfoAsync(string scheme, string accessToken, string proof = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, OidcServerPipeline.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, accessToken);
        if (proof != null)
        {
            request.Headers.Add("DPoP", proof);
        }

        var response = await _pipeline.BackChannelClient.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_request_with_proof_returns_a_DPoP_bound_token()
    {
        var (status, json) = await ClientCredentialsAsync(proofs: _key.Create("POST", TokenUrl));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("DPoP", json["token_type"].ToString());
        Assert.Equal(_key.Thumbprint, Jkt(json["access_token"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_request_without_proof_returns_a_bearer_token()
    {
        var (status, json) = await ClientCredentialsAsync();

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Bearer", json["token_type"].ToString());
        Assert.Null(Jkt(json["access_token"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Client_that_requires_DPoP_cannot_get_a_bearer_token()
    {
        var (status, json) = await ClientCredentialsAsync("cc_required");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(OidcConstants.TokenErrors.InvalidRequest, json["error"].ToString());
    }

    public static TheoryData<string> InvalidProofCases => new()
    {
        "wrong_method", "wrong_url", "old_iat", "future_iat", "wrong_type", "private_jwk", "wrong_signature", "repeated_header",
        "malformed", "symmetric_algorithm", "no_jwk", "no_jti", "no_iat", "no_htm", "http_scheme", "other_port"
    };

    [Theory]
    [Trait("Category", Category)]
    [MemberData(nameof(InvalidProofCases))]
    public async Task Invalid_proof_is_rejected(string @case)
    {
        string[] proofs = @case switch
        {
            "wrong_method" => [_key.Create("GET", TokenUrl)],
            "wrong_url" => [_key.Create("POST", "https://server/connect/other")],
            "old_iat" => [_key.Create("POST", TokenUrl, issuedAt: DateTimeOffset.UtcNow.AddMinutes(-5))],
            "future_iat" => [_key.Create("POST", TokenUrl, issuedAt: DateTimeOffset.UtcNow.AddMinutes(5))],
            "wrong_type" => [_key.Create("POST", TokenUrl, type: "JWT")],
            "private_jwk" => [_key.Create("POST", TokenUrl, jwk: _key.PrivateJwk())],
            "wrong_signature" => [_key.Create("POST", TokenUrl, signingKey: _otherKey.SigningKey)],
            "repeated_header" => [_key.Create("POST", TokenUrl), _key.Create("POST", TokenUrl)],
            "malformed" => ["not-a-jwt"],
            "symmetric_algorithm" => [_key.Create("POST", TokenUrl, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256))],
            "no_jwk" => [_key.Create("POST", TokenUrl, includeJwk: false)],
            "no_jti" => [_key.Create("POST", TokenUrl, omitClaims: ["jti"])],
            "no_iat" => [_key.Create("POST", TokenUrl, omitClaims: ["iat"])],
            "no_htm" => [_key.Create("POST", TokenUrl, omitClaims: ["htm"])],
            "http_scheme" => [_key.Create("POST", "http://server/connect/token")],
            "other_port" => [_key.Create("POST", "https://server:8443/connect/token")],
            _ => throw new ArgumentOutOfRangeException(nameof(@case))
        };

        var (status, json) = await ClientCredentialsAsync(proofs: proofs);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, json["error"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Replayed_proof_is_rejected()
    {
        var proof = _key.Create("POST", TokenUrl);
        Assert.Equal(HttpStatusCode.OK, (await ClientCredentialsAsync(proofs: proof)).Status);

        var (status, json) = await ClientCredentialsAsync(proofs: proof);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, json["error"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_bound_with_dpop_jkt_requires_a_proof_of_that_key()
    {
        var code = await AuthorizeAsync("web", _key.Thumbprint);

        var wrongKey = await RedeemAsync("web", code, _otherKey.Create("POST", TokenUrl));
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, wrongKey.Json["error"].ToString());

        var code2 = await AuthorizeAsync("web", _key.Thumbprint);
        var noProof = await RedeemAsync("web", code2, null);
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, noProof.Json["error"].ToString());

        var code3 = await AuthorizeAsync("web", _key.Thumbprint);
        var ok = await RedeemAsync("web", code3, _key.Create("POST", TokenUrl));
        Assert.Equal(HttpStatusCode.OK, ok.Status);
        Assert.Equal(_key.Thumbprint, Jkt(ok.Json["access_token"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Refresh_token_of_a_public_client_stays_bound_to_its_DPoP_key()
    {
        var code = await AuthorizeAsync("spa");
        var token = await RedeemAsync("spa", code, _key.Create("POST", TokenUrl));
        var refreshToken = token.Json["refresh_token"].ToString();

        var otherKey = await RefreshAsync("spa", refreshToken, _otherKey.Create("POST", TokenUrl));
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, otherKey.Json["error"].ToString());

        var noProof = await RefreshAsync("spa", refreshToken, null);
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, noProof.Json["error"].ToString());

        var sameKey = await RefreshAsync("spa", refreshToken, _key.Create("POST", TokenUrl));
        Assert.Equal(HttpStatusCode.OK, sameKey.Status);
        Assert.Equal("DPoP", sameKey.Json["token_type"].ToString());
        Assert.Equal(_key.Thumbprint, Jkt(sameKey.Json["access_token"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Confidential_client_can_bind_refreshed_tokens_to_a_new_key()
    {
        var code = await AuthorizeAsync("web");
        var token = await RedeemAsync("web", code, _key.Create("POST", TokenUrl));

        var refreshed = await RefreshAsync("web", token.Json["refresh_token"].ToString(), _otherKey.Create("POST", TokenUrl));

        Assert.Equal(HttpStatusCode.OK, refreshed.Status);
        Assert.Equal(_otherKey.Thumbprint, Jkt(refreshed.Json["access_token"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Userinfo_requires_the_DPoP_scheme_and_a_proof_for_a_bound_token()
    {
        var code = await AuthorizeAsync("web");
        var token = await RedeemAsync("web", code, _key.Create("POST", TokenUrl));
        var accessToken = token.Json["access_token"].ToString();
        var userInfoUrl = OidcServerPipeline.UserInfoEndpoint;

        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("Bearer", accessToken)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("DPoP", accessToken)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("DPoP", accessToken, _key.Create("GET", userInfoUrl))));
        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("DPoP", accessToken, _otherKey.Create("GET", userInfoUrl, accessToken))));
        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("DPoP", accessToken, _key.Create("GET", userInfoUrl, "another-access-token"))));
        Assert.Equal(HttpStatusCode.OK, (await UserInfoAsync("DPoP", accessToken, _key.Create("GET", userInfoUrl, accessToken))));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Userinfo_rejects_a_bearer_token_sent_with_the_DPoP_scheme()
    {
        var code = await AuthorizeAsync("web");
        var token = await RedeemAsync("web", code, null);
        var accessToken = token.Json["access_token"].ToString();

        Assert.Equal(HttpStatusCode.Unauthorized, (await UserInfoAsync("DPoP", accessToken, _key.Create("GET", OidcServerPipeline.UserInfoEndpoint, accessToken))));
        Assert.Equal(HttpStatusCode.OK, (await UserInfoAsync("Bearer", accessToken)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Proof_at_the_pushed_authorization_endpoint_binds_the_code()
    {
        await _pipeline.LoginAsync("bob");
        var (status, json) = await PostFormAsync(ParUrl, new Dictionary<string, string>
        {
            { "client_id", "web" },
            { "client_secret", "secret" },
            { "response_type", "code" },
            { "scope", "openid api" },
            { "redirect_uri", RedirectUri }
        }, _key.Create("POST", ParUrl));
        Assert.Equal(HttpStatusCode.Created, status);

        var authorize = await _pipeline.BrowserClient.GetAsync(
            OidcServerPipeline.AuthorizeEndpoint + "?client_id=web&request_uri=" + Uri.EscapeDataString(json["request_uri"].ToString()),
            TestContext.Current.CancellationToken);
        var code = new AuthorizeResponse(authorize.RequestMessage.RequestUri.ToString()).Code;

        var redeemed = await RedeemAsync("web", code, _otherKey.Create("POST", TokenUrl));
        Assert.Equal(OidcConstants.TokenErrors.InvalidDPoPProof, redeemed.Json["error"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Discovery_advertises_the_supported_proof_algorithms()
    {
        var json = JsonNode.Parse(await _pipeline.BackChannelClient.GetStringAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken)).AsObject();

        Assert.Contains("ES256", json["dpop_signing_alg_values_supported"].AsArray().Select(x => x.ToString()));
    }
}
