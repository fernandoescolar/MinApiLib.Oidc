namespace IdentityServer.IntegrationTests.Endpoints.EndSession;

public class EndSessionTests : IDisposable
{
    private const string Category = "End session endpoint";

    private IdentityServerPipeline _mockPipeline = new();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private Client _wsfedClient;

    public EndSessionTests()
    {
        _mockPipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = new List<string> { "openid" },
            RedirectUris = new List<string> { "https://client1/callback" },
            FrontChannelLogoutUri = "https://client1/signout",
            PostLogoutRedirectUris = new List<string> { "https://client1/signout-callback" },
        });

        _mockPipeline.Clients.Add(new Client
        {
            ClientId = "client2",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = new List<string> { "openid" },
            RedirectUris = new List<string> { "https://client2/callback" },
            FrontChannelLogoutUri = "https://client2/signout",
            PostLogoutRedirectUris = new List<string> {
                    "https://client2/signout-callback",
                    "https://client2/signout-callback2"
                },
        });

        _mockPipeline.Clients.Add(new Client
        {
            ClientId = "client3",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = new List<string> { "openid" },
            RedirectUris = new List<string> { "https://client3/callback" },
            BackChannelLogoutUri = "https://client3/signout",
        });

        _mockPipeline.Clients.Add(_wsfedClient = new Client
        {
            ClientId = "client4",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = new List<string> { "openid" },
            RedirectUris = new List<string> { "https://client4/callback" },
            FrontChannelLogoutUri = "https://client4/signout",
        });

        _mockPipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = new Claim[]
            {
                    new("name", "Bob Loblaw"),
                    new("email", "bob@loblaw.com"),
                    new("role", "Attorney")
            }
        });

        _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId()
            });

        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_not_return_404()
    {
        var response = await _mockPipeline.BackChannelClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_request_should_redirect_to_logout_page()
    {
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_redirect_to_configured_logout_path()
    {
        _mockPipeline.Options.UserInteraction.LogoutUrl = "/logout";
        _mockPipeline.Options.UserInteraction.LogoutIdParameter = "id";

        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client1/callback" },
                    { "client_id", "client1" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        response = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token +
            "&post_logout_redirect_uri=https://client1/signout-callback", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://server/logout?id=", response.Headers.Location.ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task logout_request_with_params_should_pass_values_in_logout_context()
    {
        await _mockPipeline.LoginAsync("bob");

        var authorization = await _mockPipeline.RequestAuthorizationEndpointAsync(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client2/callback" },
                    { "client_id", "client2" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token +
            "&post_logout_redirect_uri=https://client2/signout-callback2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
        Assert.NotNull(_mockPipeline.LogoutRequest);
        Assert.Equal("client2", _mockPipeline.LogoutRequest.ClientId);
        Assert.Equal("https://client2/signout-callback2", _mockPipeline.LogoutRequest.PostLogoutRedirectUri);

        var parts = _mockPipeline.LogoutRequest.SignOutIFrameUrl.Split('?');
        Assert.Equal(IdentityServerPipeline.EndSessionCallbackEndpoint, parts[0]);
        var iframeUrl = QueryHelpers.ParseNullableQuery(parts[1]);
        Assert.NotNull(iframeUrl["endSessionId"].FirstOrDefault());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task logout_request_with_params_but_user_no_longer_authenticated_should_pass_redirect_info_to_logout()
    {
        await _mockPipeline.LoginAsync("bob");

        var authorization = await _mockPipeline.RequestAuthorizationEndpointAsync(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client2/callback" },
                    { "client_id", "client2" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        _mockPipeline.RemoveLoginCookie();
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token +
            "&post_logout_redirect_uri=https://client2/signout-callback2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
        Assert.NotNull(_mockPipeline.LogoutRequest);
        Assert.Equal("client2", _mockPipeline.LogoutRequest.ClientId);
        Assert.Equal("https://client2/signout-callback2", _mockPipeline.LogoutRequest.PostLogoutRedirectUri);
        Assert.Null(_mockPipeline.LogoutRequest.SignOutIFrameUrl);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_should_support_POST()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client1/callback" },
                    { "client_id", "client1" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;

        var values = new List<KeyValuePair<string, string>>();
        values.Add(new KeyValuePair<string, string>("id_token_hint", id_token));
        values.Add(new KeyValuePair<string, string>("post_logout_redirect_uri", "https://client1/signout-callback"));
        var content = new FormUrlEncodedContent(values);
        _ = await _mockPipeline.BrowserClient.PostAsync(IdentityServerPipeline.EndSessionEndpoint, content, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
        Assert.NotNull(_mockPipeline.LogoutRequest);
        Assert.Equal("client1", _mockPipeline.LogoutRequest.ClientId);
        Assert.Equal("https://client1/signout-callback", _mockPipeline.LogoutRequest.PostLogoutRedirectUri);

        var parts = _mockPipeline.LogoutRequest.SignOutIFrameUrl.Split('?');
        Assert.Equal(IdentityServerPipeline.EndSessionCallbackEndpoint, parts[0]);
        var iframeUrl = QueryHelpers.ParseNullableQuery(parts[1]);
        Assert.NotNull(iframeUrl["endSessionId"].FirstOrDefault());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_callback_without_params_should_return_400()
    {
        var response = await _mockPipeline.BackChannelClient.GetAsync(IdentityServerPipeline.EndSessionCallbackEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_callback_with_mismatched_post_logout_redirect_uri_should_not_pass_along_logout_uri()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client1/callback" },
                    { "client_id", "client1" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token +
            "&post_logout_redirect_uri=https://client1/signout-callback-not-valid", cancellationToken: TestContext.Current.CancellationToken);

        var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;

        _ = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LogoutRequest.ClientId);
        Assert.Null(_mockPipeline.LogoutRequest.PostLogoutRedirectUri);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_callback_with_mismatched_id_token_hint_should_not_pass_along_logout_message()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client1/callback" },
                    { "client_id", "client1" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        await _mockPipeline.LoginAsync("alice");

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token +
            "&post_logout_redirect_uri=https://client1/signout-callback", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(_mockPipeline.LogoutRequest.ClientId);
        Assert.Null(_mockPipeline.LogoutRequest.PostLogoutRedirectUri);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_signout_callback_should_return_200_html()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;

        var response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_signout_callback_should_render_iframes_for_all_clients()
    {
        await _mockPipeline.LoginAsync("bob");
        var sid = _mockPipeline.GetSessionCookie().Value;

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        var url2 = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url2, cancellationToken: TestContext.Current.CancellationToken);

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;

        var response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl, cancellationToken: TestContext.Current.CancellationToken);

        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(HtmlEncoder.Default.Encode("https://client1/signout?sid=" + sid + "&iss=" + UrlEncoder.Default.Encode("https://server")), html);
        Assert.Contains(HtmlEncoder.Default.Encode("https://client2/signout?sid=" + sid + "&iss=" + UrlEncoder.Default.Encode("https://server")), html);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signout_callback_should_use_signoutcleanup_for_wsfed_client()
    {
        await _mockPipeline.LoginAsync("bob");
        _ = _mockPipeline.GetSessionCookie().Value;

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client4",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client4/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        var signoutFrameUrl = _mockPipeline.LogoutRequest.SignOutIFrameUrl;

        _wsfedClient.ProtocolType = ProtocolTypes.WsFederation;

        var response = await _mockPipeline.BrowserClient.GetAsync(signoutFrameUrl, cancellationToken: TestContext.Current.CancellationToken);

        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("https://client4/signout?wa=wsignoutcleanup1.0", html);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_id_token_hint_but_no_post_logout_redirect_uri_should_not_use_single_registered_post_logout_redirect_uri()
    {
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client1/callback" },
                    { "client_id", "client1" },
            }), cancellationToken: TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        _ = await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(_mockPipeline.LogoutRequest.PostLogoutRedirectUri);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task valid_id_token_hint_but_no_post_logout_redirect_uri_should_not_use_any_of_multiple_registered_post_logout_redirect_uri()
    {
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);
        var authorization = new AuthorizeResponse(response.Headers.Location.ToString());
        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(IdentityServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client2/callback" },
                    { "client_id", "client2" },
            }), TestContext.Current.CancellationToken);
        var tokenObject = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var id_token = tokenObject["id_token"].ToString();

        _mockPipeline.BrowserClient.AllowAutoRedirect = true;
        await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint +
            "?id_token_hint=" + id_token, TestContext.Current.CancellationToken);

        Assert.Null(_mockPipeline.LogoutRequest.PostLogoutRedirectUri);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task logout_with_clients_should_render_signout_callback_iframe()
    {
        await _mockPipeline.LoginAsync("bob");

        var response = await _mockPipeline.RequestAuthorizationEndpointAsync(
            clientId: "client2",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        Assert.NotNull(response);

        await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
        Assert.NotNull(_mockPipeline.LogoutRequest.SignOutIFrameUrl);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task logout_without_clients_should_not_render_signout_callback_iframe()
    {
        await _mockPipeline.LoginAsync("bob");

        await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LogoutWasCalled);
        Assert.Null(_mockPipeline.LogoutRequest.SignOutIFrameUrl);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task logout_should_invoke_back_channel_logout()
    {
        _mockPipeline.BackChannelMessageHandler.OnInvoke = async req =>
        {
            Assert.StartsWith("https://client3/signout", req.RequestUri.ToString());

            var form = await req.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Contains(OidcConstants.BackChannelLogoutRequest.LogoutToken, form);

            var token = form.Split('=')[1];
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);

            var header = JsonNode.Parse(Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[0]))).AsObject();
            Assert.Equal("logout+jwt", header["typ"].ToString());

            var bytes = Base64Url.DecodeFromChars(parts[1]);
            var json = Encoding.UTF8.GetString(bytes);
            var payload = JsonNode.Parse(json).AsObject();
            Assert.Equal("https://server", payload["iss"].ToString());
            Assert.Equal("bob", payload["sub"].ToString());
            Assert.Equal("client3", payload["aud"].ToString());
            Assert.NotNull(payload["iat"]);
            Assert.NotNull(payload["jti"]);
            Assert.NotNull(payload["sid"]);
            Assert.IsType<JsonObject>(payload["events"]);

            var events = payload["events"].AsObject();
            Assert.Single(events);
            Assert.NotNull(events["http://schemas.openid.net/event/backchannel-logout"]);
            Assert.IsType<JsonObject>(events["http://schemas.openid.net/event/backchannel-logout"]);

            var evt = events["http://schemas.openid.net/event/backchannel-logout"].AsObject();
            Assert.Empty(evt);
        };

        await _mockPipeline.LoginAsync("bob");

        var response = await _mockPipeline.RequestAuthorizationEndpointAsync(
            clientId: "client3",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce");
        Assert.NotNull(response);

        await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.BackChannelMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task back_channel_logout_should_not_affect_end_session_callback()
    {
        _mockPipeline.BackChannelMessageHandler.OnInvoke = req => throw new Exception("boom!");

        await _mockPipeline.LoginAsync("bob");

        var response = await _mockPipeline.RequestAuthorizationEndpointAsync(
            clientId: "client3",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce");
        Assert.NotNull(response);

        await _mockPipeline.BrowserClient.GetAsync(IdentityServerPipeline.EndSessionEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.BackChannelMessageHandler.InvokeWasCalled);
    }
}
