namespace MinApiLib.Oidc.JwtBearer.Tests;

/// <summary>
/// An API at https://api with JWT bearer authentication against <see cref="TestOidcServer"/> and
/// <c>AddDPoP</c>. <c>GET /orders</c> answers how the request was authenticated.
/// </summary>
public sealed class TestApi : IAsyncDisposable
{
    public const string Url = "https://api";

    private readonly IHost _host;
    private readonly TestServer _server;

    private TestApi(IHost host)
    {
        _host = host;
        _server = host.GetTestServer();
    }

    /// <summary>The requests that reached the API through <see cref="CreateHandler"/>.</summary>
    public RequestLog Log { get; } = new();

    public static async Task<TestApi> StartAsync(TestOidcServer server, Action<DPoPJwtBearerOptions>? configure = null)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthorization();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.Authority = TestOidcServer.Url;
                            options.BackchannelHttpHandler = server.CreateHandler();
                            options.MapInboundClaims = false;
                            options.Challenge = "Bearer realm=\"orders\"";
                            options.TokenValidationParameters.ValidateAudience = false;
                            options.TokenValidationParameters.ValidTypes = ["at+jwt"];
                        })
                        .AddDPoP(configure);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapMethods("/orders", ["GET", "POST"], (HttpContext http) => Results.Ok(new
                    {
                        client_id = http.User.FindFirst("client_id")?.Value,
                        scheme = http.GetAccessTokenScheme(),
                        jkt = http.GetDPoPThumbprint()
                    })).RequireAuthorization());
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        return new TestApi(host);
    }

    /// <summary>A handler that sends requests to the API and records them.</summary>
    public HttpMessageHandler CreateHandler() => Handlers.Recording(Log, _server.CreateHandler());

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}
