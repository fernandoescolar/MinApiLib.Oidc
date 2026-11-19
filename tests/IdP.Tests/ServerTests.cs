namespace IdP.Tests;

public sealed class ServerTests(IdPFixture fixture)
{
    [Fact]
    public async Task Discovery_document_describes_the_server()
    {
        using var client = fixture.CreateBrowser();
        var discovery = await client.GetJson("/.well-known/openid-configuration");

        Assert.Equal("http://localhost", discovery.GetProperty("issuer").GetString());
        Assert.Equal("http://localhost/connect/authorize", discovery.GetProperty("authorization_endpoint").GetString());
        Assert.Equal("http://localhost/connect/token", discovery.GetProperty("token_endpoint").GetString());
        Assert.Equal("http://localhost/connect/deviceauthorization", discovery.GetProperty("device_authorization_endpoint").GetString());
        var scopes = discovery.GetProperty("scopes_supported").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Contains("openid", scopes);
        Assert.Contains("roles", scopes);
        Assert.Contains("OidcServerApi", scopes);
        Assert.Contains("orders.read", scopes);
    }

    [Fact]
    public async Task Health_is_ok_with_the_database()
    {
        using var client = fixture.CreateBrowser();
        var response = await client.Get("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_created_the_administrator_and_the_standard_configuration()
    {
        var users = fixture.Mongo.GetCollection<IdP.Users.IdPUser>("users");
        var admin = await users.Find(u => u.UserName == IdPFixture.AdminUserName).FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(admin);
        Assert.Contains("admin", admin.Roles);
        Assert.True(admin.EmailConfirmed);

        var roles = fixture.Mongo.GetCollection<IdP.Users.IdPRole>("roles");
        Assert.True((await roles.Find(r => r.Name == "admin").FirstAsync(TestContext.Current.CancellationToken)).BuiltIn);

        var resources = fixture.Mongo.GetCollection<MinApiLib.Oidc.Server.Core.Models.IdentityResource>("identityResources");
        var names = await resources.Find(FilterDefinition<MinApiLib.Oidc.Server.Core.Models.IdentityResource>.Empty).Project(r => r.Name).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["email", "openid", "phone", "profile", "roles"], names.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Pages_render_the_frontend_with_their_model()
    {
        using var client = fixture.CreateBrowser();
        var response = await client.Get("/login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (appId, model) = await Pages.ReadAsync(response);
        Assert.Equal("login", appId);
        Assert.True(model.GetProperty("registrationEnabled").GetBoolean());
        Assert.Equal("Test IdP", model.GetProperty("product").GetProperty("name").GetString());

        var register = await client.Get("/register");
        Assert.Equal("register", (await Pages.ReadAsync(register)).AppId);

        var account = await client.Get("/account/profile");
        Assert.Equal(HttpStatusCode.Redirect, account.StatusCode);
        Assert.StartsWith("/login?returnUrl=", account.Headers.Location!.ToString(), StringComparison.Ordinal);
    }
}
