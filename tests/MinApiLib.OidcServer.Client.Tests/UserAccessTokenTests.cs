namespace MinApiLib.OidcServer.Client.Tests;

/// <summary>
/// The signed-in user's tokens: a web application signs in with MinApiLib.OidcServer, through a browser, and calls
/// the server's API with the user's token.
/// </summary>
public sealed class UserAccessTokenTests : IAsyncDisposable
{
    private TestOidcServer? _server;
    private TestWebApp? _web;
    private Browser? _browser;

    public async ValueTask DisposeAsync()
    {
        _browser?.Dispose();
        if (_web != null)
        {
            await _web.DisposeAsync();
        }

        if (_server != null)
        {
            await _server.DisposeAsync();
        }
    }

    private async Task<Browser> StartAsync(TimeSpan? refreshBeforeExpiration = null)
    {
        _server = await TestOidcServer.StartAsync();
        _web = await TestWebApp.StartAsync(_server, refreshBeforeExpiration);
        _browser = new Browser(_server, _web);
        return _browser;
    }

    private static async Task<JsonElement> JsonAsync(Browser browser, string path)
    {
        using var response = await browser.GetAsync(TestWebApp.Url + path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static JsonElement Api(JsonElement call)
    {
        Assert.Equal(200, call.GetProperty("status").GetInt32());
        return JsonElement.Parse(call.GetProperty("body").GetString()!);
    }

    [Fact]
    public async Task Bearer_scheme_sends_the_user_token()
    {
        var browser = await StartAsync();
        await browser.SignInAsync("oidc");

        var calls = await JsonAsync(browser, "/call");
        var token = await JsonAsync(browser, "/token");

        var api = Api(calls[0]);
        Assert.Equal("bob", api.GetProperty("sub").GetString());
        Assert.Equal("web", api.GetProperty("client_id").GetString());
        Assert.Equal("Bearer", api.GetProperty("scheme").GetString());
        Assert.Equal("Bearer", token.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, token.GetProperty("jkt").ValueKind);
        Assert.All(_server!.Log.Requests, r => Assert.Null(r.DPoP));
    }

    [Fact]
    public async Task DPoP_scheme_binds_the_code_and_the_tokens_to_the_session_key()
    {
        var browser = await StartAsync();
        await browser.SignInAsync("oidc-dpop");

        var calls = await JsonAsync(browser, "/call?times=2");
        var token = await JsonAsync(browser, "/token");
        var jkt = token.GetProperty("jkt").GetString();

        Assert.Equal("DPoP", token.GetProperty("type").GetString());
        Assert.NotNull(jkt);

        // The authorization request (pushed or not) carried the thumbprint, and the code redemption a proof of the key.
        var authorizeRequests = _server!.Log.Requests.Where(r => r.Path is "/connect/par" or "/connect/authorize").ToList();
        Assert.Contains(authorizeRequests, r => r.Form("dpop_jkt") == jkt || r.Url.Query.Contains("dpop_jkt=" + jkt, StringComparison.Ordinal));
        var redemption = Assert.Single(_server.Log.To("/connect/token"));
        Assert.Equal("authorization_code", redemption.Form("grant_type"));
        Assert.NotNull(redemption.DPoP);

        // Both calls went with the DPoP scheme, a new proof each, and the API saw the token bound to the key.
        foreach (var call in calls.EnumerateArray())
        {
            var api = Api(call);
            Assert.Equal("DPoP", api.GetProperty("scheme").GetString());
            Assert.Equal(jkt, api.GetProperty("jkt").GetString());
            Assert.Equal("bob", api.GetProperty("sub").GetString());
        }

        Assert.Equal(2, _server.Log.To("/api/whoami").Select(r => new JsonWebToken(r.DPoP).Id).Distinct().Count());
    }

    [Fact]
    public async Task DPoP_token_is_refreshed_with_a_proof_and_the_session_keeps_the_new_tokens()
    {
        // Refreshing 2 hours before expiry refreshes the 1-hour tokens on every request.
        var browser = await StartAsync(refreshBeforeExpiration: TimeSpan.FromHours(2));
        await browser.SignInAsync("oidc-dpop");

        var first = await JsonAsync(browser, "/call?times=2");
        var second = await JsonAsync(browser, "/call");

        Assert.All(first.EnumerateArray().Concat(second.EnumerateArray()), call => Assert.Equal("DPoP", Api(call).GetProperty("scheme").GetString()));

        // One refresh per request, each with a proof. The second request could only refresh because the first one
        // wrote the new, one-time refresh token into the session cookie.
        var refreshes = _server!.Log.To("/connect/token").Where(r => r.Form("grant_type") == "refresh_token").ToList();
        Assert.Equal(2, refreshes.Count);
        Assert.All(refreshes, r => Assert.NotNull(r.DPoP));
        Assert.NotEqual(refreshes[0].Form("refresh_token"), refreshes[1].Form("refresh_token"));
    }

    [Fact]
    public async Task Bearer_token_is_refreshed_without_a_proof()
    {
        var browser = await StartAsync(refreshBeforeExpiration: TimeSpan.FromHours(2));
        await browser.SignInAsync("oidc");

        var calls = await JsonAsync(browser, "/call");

        Assert.Equal("Bearer", Api(calls[0]).GetProperty("scheme").GetString());
        var refresh = Assert.Single(_server!.Log.To("/connect/token"), r => r.Form("grant_type") == "refresh_token");
        Assert.Null(refresh.DPoP);
    }

    [Fact]
    public async Task No_token_without_a_signed_in_user()
    {
        var browser = await StartAsync();

        using var response = await browser.GetAsync(TestWebApp.Url + "/token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
