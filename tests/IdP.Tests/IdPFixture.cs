[assembly: AssemblyFixture(typeof(IdP.Tests.IdPFixture))]

namespace IdP.Tests;

/// <summary>
/// The identity provider on a MongoDB container, hosted in process. The frontend is replaced by a stub manifest, so
/// the pages render without a Vite build; the tests read the page models, not the React output.
/// </summary>
public sealed class IdPFixture : IAsyncLifetime
{
    public const string AdminUserName = "admin";
    public const string AdminPassword = "Admin-123456";

    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0").Build();
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "idp-tests-" + Guid.NewGuid().ToString("N"));
    private WebApplicationFactory<Program>? _factory;

    public string Database { get; } = "idp_test_" + Guid.NewGuid().ToString("N")[..8];

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public IMongoDatabase Mongo => new MongoClient(_container.GetConnectionString()).GetDatabase(Database);

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Directory.CreateDirectory(Path.Combine(_contentRoot, "wwwroot", ".vite"));
        Directory.CreateDirectory(Path.Combine(_contentRoot, "wwwroot", "assets"));
        await File.WriteAllTextAsync(Path.Combine(_contentRoot, "wwwroot", ".vite", "manifest.json"),
            """{"src/main.tsx":{"file":"assets/main.js","css":["assets/main.css"],"isEntry":true}}""");
        await File.WriteAllTextAsync(Path.Combine(_contentRoot, "wwwroot", "assets", "main.js"), "// stub");
        await File.WriteAllTextAsync(Path.Combine(_contentRoot, "wwwroot", "assets", "main.css"), "/* stub */");

        var connectionString = _container.GetConnectionString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(_contentRoot);
            builder.UseEnvironment("Production");
            // Settings go in the host configuration: Program reads them while it builds the application.
            foreach (var (key, value) in new Dictionary<string, string?>
            {
                ["ConnectionStrings:Mongo"] = connectionString,
                ["IdP:Database"] = Database,
                ["IdP:Name"] = "Test IdP",
                ["IdP:Admin:UserName"] = AdminUserName,
                ["IdP:Admin:Password"] = AdminPassword,
                ["IdP:Admin:Email"] = "admin@example.com",
                ["IdP:Registration:Enabled"] = "true",
                ["IdP:Seed:ApiScopes:0:Name"] = "orders.read",
                ["IdP:Seed:ApiScopes:0:DisplayName"] = "Read orders",
                ["Logging:LogLevel:Default"] = "Warning"
            })
            {
                builder.UseSetting(key, value);
            }
        });

        // Starts the host, and with it the bootstrap of the database.
        using var client = _factory.CreateClient();
        var health = await client.Get("/health");
        health.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
        try
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>A browser: keeps cookies, follows no redirects (the tests check them).</summary>
    public HttpClient CreateBrowser() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
}

/// <summary>Reads the pages the server renders: which app they mount and their model.</summary>
public static class Pages
{
    private static readonly Regex AppId = new("app-id=\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex AppModel = new("app-model=\"([^\"]*)\"", RegexOptions.Compiled);

    public static async Task<(string AppId, JsonElement Model)> ReadAsync(HttpResponseMessage response)
    {
        var html = await response.Text();
        var appId = AppId.Match(html).Groups[1].Value;
        var model = JsonDocument.Parse(Convert.FromBase64String(AppModel.Match(html).Groups[1].Value)).RootElement;
        return (appId, model);
    }

    public static (string FieldName, string HeaderName, string Token) Antiforgery(JsonElement model)
    {
        var antiforgery = model.GetProperty("antiforgery");
        return (antiforgery.GetProperty("fieldName").GetString()!, antiforgery.GetProperty("headerName").GetString()!, antiforgery.GetProperty("token").GetString()!);
    }
}

/// <summary>Signs users in through the login page, as a browser would.</summary>
public static class Browser
{
    public static async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string userName, string password, string? returnUrl = null)
    {
        var loginUrl = returnUrl == null ? "/login" : "/login?returnUrl=" + Uri.EscapeDataString(returnUrl);
        var page = await browser.Get(loginUrl);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var (appId, model) = await Pages.ReadAsync(page);
        Assert.Equal("login", appId);
        var (fieldName, _, token) = Pages.Antiforgery(model);

        return await browser.PostForm("/login", new Dictionary<string, string>
        {
            [fieldName] = token,
            ["username"] = userName,
            ["password"] = password,
            ["returnUrl"] = returnUrl ?? string.Empty
        });
    }

    /// <summary>A signed-in administrator, with the antiforgery header the APIs need.</summary>
    public static async Task<HttpClient> AdminAsync(IdPFixture fixture)
    {
        var browser = fixture.CreateBrowser();
        var response = await SignInAsync(browser, IdPFixture.AdminUserName, IdPFixture.AdminPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await AddAntiforgeryHeaderAsync(browser, "/admin");
        return browser;
    }

    public static async Task AddAntiforgeryHeaderAsync(HttpClient browser, string consolePath)
    {
        var console = await browser.Get(consolePath);
        Assert.Equal(HttpStatusCode.OK, console.StatusCode);
        var (_, model) = await Pages.ReadAsync(console);
        var (_, headerName, token) = Pages.Antiforgery(model);
        browser.DefaultRequestHeaders.Remove(headerName);
        browser.DefaultRequestHeaders.Add(headerName, token);
    }
}
