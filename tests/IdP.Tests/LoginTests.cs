namespace IdP.Tests;

public sealed class LoginTests(IdPFixture fixture)
{
    [Fact]
    public async Task Wrong_password_shows_the_login_page_with_an_error()
    {
        using var browser = fixture.CreateBrowser();
        var response = await Browser.SignInAsync(browser, IdPFixture.AdminUserName, "not-the-password");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (appId, model) = await Pages.ReadAsync(response);
        Assert.Equal("login", appId);
        Assert.Equal("Invalid user name or password.", model.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Right_password_signs_in_and_returns_to_the_console()
    {
        using var browser = fixture.CreateBrowser();
        var response = await Browser.SignInAsync(browser, IdPFixture.AdminUserName, IdPFixture.AdminPassword, "/account/security");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/security", response.Headers.Location!.ToString());

        var console = await browser.Get("/account/security");
        var (appId, model) = await Pages.ReadAsync(console);
        Assert.Equal("account", appId);
        Assert.Equal(IdPFixture.AdminUserName, model.GetProperty("user").GetProperty("userName").GetString());
        Assert.True(model.GetProperty("user").GetProperty("isAdmin").GetBoolean());
        Assert.Equal("pwd", model.GetProperty("user").GetProperty("amr")[0].GetString());
    }

    [Fact]
    public async Task Registration_creates_a_user_and_signs_them_in()
    {
        using var browser = fixture.CreateBrowser();
        var page = await browser.Get("/register");
        var (_, model) = await Pages.ReadAsync(page);
        var (fieldName, _, token) = Pages.Antiforgery(model);
        var userName = "carol" + Guid.NewGuid().ToString("N")[..6];

        var response = await browser.PostForm("/register", new Dictionary<string, string>
        {
            [fieldName] = token,
            ["username"] = userName,
            ["email"] = userName + "@example.com",
            ["password"] = "Carol-123456",
            ["firstName"] = "Carol",
            ["lastName"] = "Jones"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var me = await browser.GetJson("/api/account/me");
        Assert.Equal(userName, me.GetProperty("userName").GetString());
        Assert.Equal("Carol", me.GetProperty("firstName").GetString());
        Assert.False(me.GetProperty("emailConfirmed").GetBoolean());

        // A plain user is not an administrator.
        var admin = await browser.Get("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
    }

    [Fact]
    public async Task Logout_prompts_without_a_client_and_ends_the_session()
    {
        using var browser = fixture.CreateBrowser();
        await Browser.SignInAsync(browser, IdPFixture.AdminUserName, IdPFixture.AdminPassword);

        var prompt = await browser.Get("/logout");
        var (appId, model) = await Pages.ReadAsync(prompt);
        Assert.Equal("logout", appId);
        Assert.True(model.GetProperty("prompt").GetBoolean());
        var (fieldName, _, token) = Pages.Antiforgery(model);

        var done = await browser.PostForm("/logout", new Dictionary<string, string> { [fieldName] = token });
        var (_, doneModel) = await Pages.ReadAsync(done);
        Assert.False(doneModel.GetProperty("prompt").GetBoolean());

        var me = await browser.Get("/api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
