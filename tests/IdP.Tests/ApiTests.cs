namespace IdP.Tests;

public sealed class ApiTests(IdPFixture fixture)
{
    [Fact]
    public async Task State_changing_calls_need_the_antiforgery_header()
    {
        using var browser = fixture.CreateBrowser();
        await Browser.SignInAsync(browser, IdPFixture.AdminUserName, IdPFixture.AdminPassword);

        var list = await browser.Get("/api/admin/roles");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await browser.PostJson("/api/admin/roles", new { name = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Roles_groups_and_users_round_trip()
    {
        using var admin = await Browser.AdminAsync(fixture);
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var role = await (await admin.PostJson("/api/admin/roles", new { name = "editor-" + suffix, description = "Edits" })).Json();
        Assert.Equal("editor-" + suffix, role.GetProperty("name").GetString());

        var group = await (await admin.PostJson("/api/admin/groups", new { name = "staff-" + suffix, roles = new[] { "editor-" + suffix } })).Json();
        var groupId = group.GetProperty("id").GetString()!;

        var created = await admin.PostJson("/api/admin/users", new
        {
            userName = "dave-" + suffix,
            email = $"dave-{suffix}@example.com",
            emailConfirmed = true,
            firstName = "Dave",
            password = "Dave-123456",
            groups = new[] { groupId }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await created.Json();
        var userId = user.GetProperty("id").GetString()!;
        Assert.Equal(["editor-" + suffix], user.GetProperty("effectiveRoles").EnumerateArray().Select(r => r.GetString()));

        var search = await admin.GetJson("/api/admin/users?q=dave-" + suffix);
        Assert.Equal(1, search.GetProperty("total").GetInt32());

        // The group's role is a claim of the user.
        using var dave = fixture.CreateBrowser();
        var signedIn = await Browser.SignInAsync(dave, "dave-" + suffix, "Dave-123456");
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        var me = await dave.GetJson("/api/account/me");
        Assert.Contains("editor-" + suffix, me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        Assert.Contains("staff-" + suffix, me.GetProperty("groups").EnumerateArray().Select(g => g.GetString()));

        // Disabling the user ends the session.
        var disabled = await admin.PutJson($"/api/admin/users/{userId}", new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var afterwards = await dave.Get("/api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterwards.StatusCode);

        var deleted = await admin.Delete($"/api/admin/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.Get($"/api/admin/users/{userId}")).StatusCode);
    }

    [Fact]
    public async Task Clients_keep_their_secrets_hidden_and_validate_their_configuration()
    {
        using var admin = await Browser.AdminAsync(fixture);
        var clientId = "app-" + Guid.NewGuid().ToString("N")[..6];

        var invalid = await admin.PostJson("/api/admin/clients", new { clientId, allowedGrantTypes = new[] { "authorization_code" } });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); // no redirect URI

        var created = await admin.PostJson("/api/admin/clients", new
        {
            clientId,
            clientName = "An application",
            allowedGrantTypes = new[] { "authorization_code" },
            redirectUris = new[] { "https://app.example.com/signin-oidc" },
            allowedScopes = new[] { "openid", "profile" }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var client = await created.Json();
        var secret = client.GetProperty("clientSecret").GetString();
        Assert.False(string.IsNullOrEmpty(secret));
        Assert.Equal(1, client.GetProperty("secrets").GetArrayLength());
        Assert.False(client.TryGetProperty("clientSecrets", out _));

        var read = await admin.GetJson($"/api/admin/clients/{clientId}");
        Assert.False(read.TryGetProperty("clientSecret", out _));
        Assert.Equal("An application", read.GetProperty("clientName").GetString());
        Assert.Equal("oneTimeOnly", read.GetProperty("refreshTokenUsage").GetString());

        var added = await (await admin.PostJson($"/api/admin/clients/{clientId}/secrets", new { description = "second" })).Json();
        var fingerprint = added.GetProperty("secret").GetProperty("id").GetString();
        var removed = await admin.Delete($"/api/admin/clients/{clientId}/secrets/{fingerprint}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var deleted = await admin.Delete($"/api/admin/clients/{clientId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Dashboard_events_and_keys_answer()
    {
        using var admin = await Browser.AdminAsync(fixture);
        var dashboard = await admin.GetJson("/api/admin/dashboard");
        Assert.True(dashboard.GetProperty("counts").GetProperty("users").GetInt64() >= 1);
        Assert.Equal("http://localhost", dashboard.GetProperty("server").GetProperty("issuer").GetString());

        var events = await admin.GetJson("/api/admin/events?name=User%20Login%20Success");
        Assert.True(events.GetProperty("total").GetInt64() >= 1);
        Assert.Equal("Authentication", events.GetProperty("items")[0].GetProperty("category").GetString());

        var keys = await admin.GetJson("/api/admin/keys");
        Assert.Equal("http://localhost/.well-known/openid-configuration/jwks", keys.GetProperty("jwksUri").GetString());
    }

    [Fact]
    public async Task Account_changes_the_password_and_keeps_the_session()
    {
        using var admin = await Browser.AdminAsync(fixture);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var created = await admin.PostJson("/api/admin/users", new { userName = "erin-" + suffix, email = $"erin-{suffix}@example.com", password = "Erin-123456" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var erin = fixture.CreateBrowser();
        await Browser.SignInAsync(erin, "erin-" + suffix, "Erin-123456");
        await Browser.AddAntiforgeryHeaderAsync(erin, "/account");

        var wrong = await erin.PostJson("/api/account/password", new { currentPassword = "nope", newPassword = "Erin-654321" });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var changed = await erin.PostJson("/api/account/password", new { currentPassword = "Erin-123456", newPassword = "Erin-654321" });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        // Still signed in, with the renewed cookie.
        var me = await erin.Get("/api/account/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var profile = await erin.PutJson("/api/account/profile", new { firstName = "Erin", lastName = "Stone", attributes = new { department = "R&D" } });
        var updated = await profile.Json();
        Assert.Equal("Erin", updated.GetProperty("profile").GetProperty("firstName").GetString());
        Assert.Equal("R&D", updated.GetProperty("profile").GetProperty("attributes").GetProperty("department").GetString());

        using var again = fixture.CreateBrowser();
        Assert.Equal(HttpStatusCode.Redirect, (await Browser.SignInAsync(again, "erin-" + suffix, "Erin-654321")).StatusCode);
    }
}
