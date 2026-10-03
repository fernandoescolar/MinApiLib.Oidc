namespace MinApiLib.Oidc.Testing;

/// <summary>
/// A MinApiLib.Oidc.Server at https://server, with a protected API of its own (<c>GET /api/whoami</c>, local API
/// authentication, which validates DPoP proofs) and a login page that signs in "bob" at once.
/// </summary>
public sealed class TestOidcServer : IAsyncDisposable
{
    public const string Url = "https://server";

    /// <summary>The web application the "web" clients redirect to.</summary>
    public const string WebUrl = "https://web";

    public const string ApiScope = OidcServerConstants.LocalApi.ScopeName;
    public const string Secret = "secret";

    private readonly IHost _host;
    private readonly TestServer _server;

    private TestOidcServer(IHost host)
    {
        _host = host;
        _server = host.GetTestServer();
    }

    /// <summary>The requests that reached the server through <see cref="CreateHandler"/>.</summary>
    public RequestLog Log { get; } = new();

    public static async Task<TestOidcServer> StartAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddOidcServer()
                        .AddInMemoryClients(Clients)
                        .AddInMemoryIdentityResources([new IdentityResources.OpenId(), new IdentityResources.Profile()])
                        .AddInMemoryApiScopes([new ApiScope(ApiScope)])
                        .AddInMemoryApiResources([])
                        .AddDeveloperSigningCredential(persistKey: false);
                    services.AddLocalApiAuthentication();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseOidcServer();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapOidcServer();
                        endpoints.MapGet("/account/login", async (HttpContext http, string returnUrl) =>
                        {
                            await http.SignInAsync(new OidcServerUser("bob") { DisplayName = "Bob" });
                            return Results.Redirect(returnUrl);
                        });
                        endpoints.MapGet("/api/whoami", (HttpContext http) => Results.Ok(new
                        {
                            sub = http.User.FindFirst("sub")?.Value,
                            client_id = http.User.FindFirst("client_id")?.Value,
                            scheme = http.Request.Headers.Authorization.ToString().Split(' ')[0],
                            jkt = Thumbprint(http.User.FindFirst("cnf")?.Value)
                        })).RequireAuthorization(OidcServerConstants.LocalApi.PolicyName);
                    });
                }))
            .StartAsync(TestContext.Current.CancellationToken);

        return new TestOidcServer(host);
    }

    /// <summary>A handler that sends requests to the server and records them.</summary>
    public HttpMessageHandler CreateHandler() => Handlers.Recording(Log, _server.CreateHandler());

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    private static string? Thumbprint(string? cnf)
        => cnf == null ? null : JsonElement.Parse(cnf).GetProperty("jkt").GetString();

    private static IReadOnlyList<Client> Clients =>
    [
        Worker("worker", requireDPoP: false),
        Worker("worker.dpop", requireDPoP: true),
        Web("web", "", requireDPoP: false),
        Web("web.dpop", "-dpop", requireDPoP: true)
    ];

    private static Client Worker(string clientId, bool requireDPoP) => new()
    {
        ClientId = clientId,
        ClientSecrets = { new Secret(Secret.Sha256()) },
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        RequireDPoP = requireDPoP,
        AllowedScopes = { ApiScope }
    };

    private static Client Web(string clientId, string suffix, bool requireDPoP) => new()
    {
        ClientId = clientId,
        ClientSecrets = { new Secret(Secret.Sha256()) },
        AllowedGrantTypes = GrantTypes.Code,
        RequirePkce = true,
        RequireConsent = false,
        RequireDPoP = requireDPoP,
        AllowOfflineAccess = true,
        RedirectUris = { $"{WebUrl}/signin-oidc{suffix}" },
        AllowedScopes = { "openid", "profile", ApiScope }
    };
}
