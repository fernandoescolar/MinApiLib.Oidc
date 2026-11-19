using System.Security.Cryptography;
using System.Text;

namespace IdP.Tests;

/// <summary>The whole thing: a client created through the API, a user signed in at the login page, tokens and claims.</summary>
public sealed class OidcFlowTests(IdPFixture fixture)
{
    [Fact]
    public async Task Authorization_code_flow_issues_tokens_with_the_users_claims()
    {
        using var admin = await Browser.AdminAsync(fixture);
        var clientId = "flow-" + Guid.NewGuid().ToString("N")[..6];
        var created = await admin.PostJson("/api/admin/clients", new
        {
            clientId,
            clientName = "Flow test",
            allowedGrantTypes = new[] { "authorization_code" },
            redirectUris = new[] { "https://client.example.com/cb" },
            allowedScopes = new[] { "openid", "profile", "email", "roles", "OidcServerApi" },
            allowOfflineAccess = true,
            alwaysIncludeUserClaimsInIdToken = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var secret = (await created.Json()).GetProperty("clientSecret").GetString()!;

        // The browser of the user: signed in, it asks for a code.
        using var browser = fixture.CreateBrowser();
        await Browser.SignInAsync(browser, IdPFixture.AdminUserName, IdPFixture.AdminPassword);
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorize = "/connect/authorize?client_id=" + clientId + "&redirect_uri=" + Uri.EscapeDataString("https://client.example.com/cb")
            + "&response_type=code&scope=" + Uri.EscapeDataString("openid profile email roles OidcServerApi offline_access")
            + "&state=xyz&code_challenge=" + challenge + "&code_challenge_method=S256&nonce=n-1";
        var response = await browser.Get(authorize);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.StartsWith("https://client.example.com/cb?", location.ToString(), StringComparison.Ordinal);
        var query = System.Web.HttpUtility.ParseQueryString(location.Query);
        var code = query["code"];
        Assert.NotNull(code);
        Assert.Equal("xyz", query["state"]);

        // The client redeems the code.
        using var client = fixture.Factory.CreateClient();
        var tokens = await client.PostForm("/connect/token", new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = "https://client.example.com/cb",
            ["client_id"] = clientId,
            ["client_secret"] = secret,
            ["code_verifier"] = verifier
        });
        Assert.Equal(HttpStatusCode.OK, tokens.StatusCode);
        var body = await tokens.Json();
        var accessToken = body.GetProperty("access_token").GetString()!;
        Assert.True(body.TryGetProperty("refresh_token", out _));
        var idToken = body.GetProperty("id_token").GetString()!;
        var idClaims = JsonDocument.Parse(Base64UrlDecode(idToken.Split('.')[1])).RootElement;
        Assert.Equal("admin", idClaims.GetProperty("preferred_username").GetString());
        Assert.Equal("admin@example.com", idClaims.GetProperty("email").GetString());
        Assert.Equal("admin", idClaims.GetProperty("role").GetString());
        Assert.Equal("local", idClaims.GetProperty("idp").GetString());
        Assert.Equal("pwd", idClaims.GetProperty("amr")[0].GetString());

        // userinfo, and the administration API with the token (the OidcServerApi scope puts the role in it).
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userinfo = await client.GetJson("/connect/userinfo");
        Assert.Equal("admin", userinfo.GetProperty("preferred_username").GetString());

        var users = await client.Get("/api/admin/users");
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);

        // With a bearer token no antiforgery header is needed.
        var role = await client.PostJson("/api/admin/roles", new { name = "from-token-" + clientId });
        Assert.Equal(HttpStatusCode.Created, role.StatusCode);
    }

    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
    }
}
