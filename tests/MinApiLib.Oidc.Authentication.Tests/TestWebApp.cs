namespace MinApiLib.Oidc.Authentication.Tests;

/// <summary>
/// A web application at https://web that signs users in with the test server, with two OpenID Connect schemes:
/// "oidc" (client "web", bearer tokens) and "oidc-dpop" (client "web.dpop", DPoP). <c>GET /call</c> calls the
/// server's API with the user's token; <c>GET /token</c> shows the token.
/// </summary>
public sealed class TestWebApp : IAsyncDisposable
{
    public const string Url = TestOidcServer.WebUrl;

    private readonly IHost _host;
    private readonly TestServer _server;

    private TestWebApp(IHost host)
    {
        _host = host;
        _server = host.GetTestServer();
    }

    public HttpMessageHandler CreateHandler() => _server.CreateHandler();

    public static async Task<TestWebApp> StartAsync(TestOidcServer server, TimeSpan? refreshBeforeExpiration = null)
    {
        var refreshBefore = refreshBeforeExpiration ?? TimeSpan.FromMinutes(1);
        var backchannel = server.CreateHandler();

        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthentication(options =>
                        {
                            options.DefaultScheme = "cookie";
                            options.DefaultChallengeScheme = "oidc";
                        })
                        .AddCookie("cookie")
                        .AddOpenIdConnect("oidc", options => Configure(options, "web", string.Empty, backchannel))
                        .AddOpenIdConnect("oidc-dpop", options => Configure(options, "web.dpop", "-dpop", backchannel));

                    services.AddOpenIdConnectTokens("oidc", options => options.RefreshBeforeExpiration = refreshBefore);
                    services.AddOpenIdConnectTokens("oidc-dpop", options =>
                    {
                        options.UseDPoP = true;
                        options.RefreshBeforeExpiration = refreshBefore;
                    });

                    services.AddHttpClient("api", client => client.BaseAddress = new Uri(TestOidcServer.Url))
                        .ConfigurePrimaryHttpMessageHandler(server.CreateHandler)
                        .AddUserAccessTokenHandler();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/login", (string scheme) => Results.Challenge(new AuthenticationProperties { RedirectUri = "/done" }, [scheme]));
                        endpoints.MapGet("/done", () => "signed in");
                        endpoints.MapGet("/call", async (IHttpClientFactory clients, int? times) =>
                        {
                            var http = clients.CreateClient("api");
                            var results = new List<object>();
                            for (var i = 0; i < (times ?? 1); i++)
                            {
                                using var response = await http.GetAsync("/api/whoami");
                                var body = await response.Content.ReadAsStringAsync();
                                results.Add(new { status = (int)response.StatusCode, body });
                            }

                            return Results.Ok(results);
                        });
                        endpoints.MapGet("/token", async (HttpContext http, IUserAccessTokenManager tokens) =>
                        {
                            var token = await tokens.GetAccessTokenAsync(http);
                            return token == null
                                ? Results.Unauthorized()
                                : Results.Ok(new { value = token.Value, type = token.TokenType, jkt = token.DPoPKey?.Thumbprint, expiresAt = token.ExpiresAt });
                        });
                    });
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        return new TestWebApp(host);
    }

    private static void Configure(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions options, string clientId, string suffix, HttpMessageHandler backchannel)
    {
        options.Authority = TestOidcServer.Url;
        options.ClientId = clientId;
        options.ClientSecret = TestOidcServer.Secret;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.CallbackPath = "/signin-oidc" + suffix;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add(TestOidcServer.ApiScope);
        options.Scope.Add("offline_access");
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.BackchannelHttpHandler = backchannel;
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
