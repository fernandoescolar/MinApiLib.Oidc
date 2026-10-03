namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.PushedAuthorization;

/// <summary>
/// Pushed authorization requests (RFC 9126).
/// </summary>
public class PushedAuthorizationTests : IDisposable
{
    private const string Category = "Pushed authorization";
    private const string ParEndpoint = OidcServerPipeline.BaseUrl + "/connect/par";
    private const string RedirectUri = "https://client1/callback";
    private const string RequestUriPrefix = "urn:ietf:params:oauth:request_uri:";

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private readonly OidcServerPipeline _pipeline = new();
    private readonly MutableClock _clock = new();

    public PushedAuthorizationTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequireConsent = false,
            RequirePkce = false,
            AllowedScopes = { "openid", "api" },
            RedirectUris = { RedirectUri }
        });
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client2",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequireConsent = false,
            RequirePkce = false,
            RequirePushedAuthorization = true,
            AllowedScopes = { "openid" },
            RedirectUris = { "https://client2/callback" }
        });
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.OnPostConfigureServices += services => services.AddSingleton<TimeProvider>(_clock);
        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Dictionary<string, string> DefaultParameters(string clientId = "client1", string redirectUri = RedirectUri) => new()
    {
        { "client_id", clientId },
        { "client_secret", "secret" },
        { "response_type", "code" },
        { "scope", "openid api" },
        { "redirect_uri", redirectUri },
        { "state", "pushed_state" },
        { "nonce", "pushed_nonce" }
    };

    private async Task<(HttpStatusCode Status, JsonObject Json)> PushAsync(Dictionary<string, string> parameters)
    {
        var response = await _pipeline.BackChannelClient.PostAsync(ParEndpoint, new FormUrlEncodedContent(parameters), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return (response.StatusCode, JsonNode.Parse(body).AsObject());
    }

    private async Task<string> PushRequestUriAsync(Dictionary<string, string> parameters = null)
    {
        var (status, json) = await PushAsync(parameters ?? DefaultParameters());
        Assert.Equal(HttpStatusCode.Created, status);
        return json["request_uri"].ToString();
    }

    private static string AuthorizeUrl(string clientId, string requestUri)
        => OidcServerPipeline.AuthorizeEndpoint + "?client_id=" + Uri.EscapeDataString(clientId) + "&request_uri=" + Uri.EscapeDataString(requestUri);

    private async Task<AuthorizeResponse> AuthorizeWithRequestUriAsync(string clientId, string requestUri)
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl(clientId, requestUri), TestContext.Current.CancellationToken);
        var url = response.RequestMessage.RequestUri.ToString();
        return url.StartsWith(OidcServerPipeline.ErrorPage, StringComparison.Ordinal) ? null : new AuthorizeResponse(url);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Push_returns_a_one_time_request_uri_with_its_lifetime()
    {
        var (status, json) = await PushAsync(DefaultParameters());

        Assert.Equal(HttpStatusCode.Created, status);
        Assert.StartsWith(RequestUriPrefix, json["request_uri"].ToString());
        Assert.Equal(600, json["expires_in"].GetValue<int>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Authorize_with_pushed_request_uri_uses_the_pushed_parameters()
    {
        await _pipeline.LoginAsync("bob");
        var requestUri = await PushRequestUriAsync();

        var authorize = await AuthorizeWithRequestUriAsync("client1", requestUri);

        Assert.NotNull(authorize);
        Assert.NotNull(authorize.Code);
        Assert.Equal("pushed_state", authorize.State);

        var token = await _pipeline.BackChannelClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = "client1",
            ClientSecret = "secret",
            Code = authorize.Code,
            RedirectUri = RedirectUri
        }, TestContext.Current.CancellationToken);
        Assert.False(token.IsError);
        Assert.Contains(new JwtSecurityTokenHandler().ReadJwtToken(token.IdentityToken).Claims, c => c.Type == "nonce" && c.Value == "pushed_nonce");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_uri_cannot_be_used_twice()
    {
        await _pipeline.LoginAsync("bob");
        var requestUri = await PushRequestUriAsync();
        Assert.NotNull((await AuthorizeWithRequestUriAsync("client1", requestUri)));

        var second = await AuthorizeWithRequestUriAsync("client1", requestUri);

        Assert.Null(second);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequestUri, _pipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Request_uri_of_another_client_is_rejected()
    {
        await _pipeline.LoginAsync("bob");
        var requestUri = await PushRequestUriAsync();

        var authorize = await AuthorizeWithRequestUriAsync("client2", requestUri);

        Assert.Null(authorize);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequestUri, _pipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Expired_request_uri_is_rejected()
    {
        await _pipeline.LoginAsync("bob");
        var requestUri = await PushRequestUriAsync();
        _clock.Now = _clock.Now.AddSeconds(601);

        var authorize = await AuthorizeWithRequestUriAsync("client1", requestUri);

        Assert.Null(authorize);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequestUri, _pipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Login_round_trip_keeps_pushed_parameters_out_of_the_browser()
    {
        var requestUri = await PushRequestUriAsync();
        _pipeline.BrowserClient.AllowAutoRedirect = false;

        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("client1", requestUri), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location.ToString();
        Assert.StartsWith(OidcServerPipeline.LoginPage, location, StringComparison.OrdinalIgnoreCase);
        var returnUrl = QueryHelpers.ParseQuery(new Uri(location).Query)["returnUrl"].ToString();
        Assert.Contains("request_uri=", returnUrl);
        Assert.Contains("client_id=client1", returnUrl);
        Assert.DoesNotContain("redirect_uri", returnUrl);
        Assert.DoesNotContain("pushed_state", returnUrl);
        Assert.DoesNotContain("scope", returnUrl);

        _pipeline.BrowserClient.AllowAutoRedirect = true;
        _pipeline.Subject = new OidcServerUser("bob").CreatePrincipal();
        var final = await _pipeline.BrowserClient.GetAsync(location, TestContext.Current.CancellationToken);

        Assert.Equal("client1", _pipeline.LoginRequest.Client.ClientId);
        var authorize = new AuthorizeResponse(final.RequestMessage.RequestUri.ToString());
        Assert.NotNull(authorize.Code);
        Assert.Equal("pushed_state", authorize.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Pushed_prompt_login_is_processed_once_and_does_not_loop()
    {
        await _pipeline.LoginAsync("bob");
        var parameters = DefaultParameters();
        parameters.Add("prompt", "login");
        var requestUri = await PushRequestUriAsync(parameters);

        _pipeline.Subject = new OidcServerUser("bob").CreatePrincipal();
        var authorize = await AuthorizeWithRequestUriAsync("client1", requestUri);

        Assert.True(_pipeline.LoginWasCalled);
        Assert.NotNull(authorize);
        Assert.NotNull(authorize.Code);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Client_that_requires_pushed_authorization_cannot_use_a_plain_authorize_request()
    {
        await _pipeline.LoginAsync("bob");

        var authorize = await _pipeline.RequestAuthorizationEndpointAsync("client2", "code", "openid", "https://client2/callback", state: "state", nonce: "nonce");

        Assert.Null(authorize);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _pipeline.ErrorMessage.Error);

        var requestUri = await PushRequestUriAsync(new Dictionary<string, string>
        {
            { "client_id", "client2" },
            { "client_secret", "secret" },
            { "response_type", "code" },
            { "scope", "openid" },
            { "redirect_uri", "https://client2/callback" }
        });
        Assert.NotNull((await AuthorizeWithRequestUriAsync("client2", requestUri)).Code);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Push_with_client_id_of_another_client_is_rejected()
    {
        // Authenticated as client2 with parameters that would be valid for client2, but naming client1.
        var parameters = new Dictionary<string, string>
        {
            { "client_id", "client1" },
            { "response_type", "code" },
            { "scope", "openid" },
            { "redirect_uri", "https://client2/callback" }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, ParEndpoint) { Content = new FormUrlEncodedContent(parameters) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("client2:secret")));

        var response = await _pipeline.BackChannelClient.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(OidcConstants.AuthorizeErrors.InvalidRequest, (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Push_with_request_uri_is_rejected()
    {
        var parameters = DefaultParameters();
        parameters.Add("request_uri", RequestUriPrefix + "abc");

        var (status, json) = await PushAsync(parameters);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, json["error"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Push_with_unregistered_redirect_uri_is_rejected()
    {
        var (status, json) = await PushAsync(DefaultParameters(redirectUri: "https://attacker/callback"));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.False(string.IsNullOrEmpty(json["error"].ToString()));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Push_without_valid_client_authentication_is_unauthorized()
    {
        var parameters = DefaultParameters();
        parameters["client_secret"] = "wrong";

        var (status, json) = await PushAsync(parameters);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal(OidcConstants.TokenErrors.InvalidClient, json["error"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Discovery_advertises_the_endpoint_and_the_requirement()
    {
        _pipeline.Options.PushedAuthorization.Required = true;

        var json = JsonNode.Parse(await _pipeline.BackChannelClient.GetStringAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken)).AsObject();

        Assert.Equal(ParEndpoint, json["pushed_authorization_request_endpoint"].ToString());
        Assert.True(json["require_pushed_authorization_requests"].GetValue<bool>());
    }
}
