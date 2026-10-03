using MinApiLib.Oidc.Server.Features.Authorize;
using MinApiLib.Oidc.Server.Features.DeviceFlow;
using MinApiLib.Oidc.Server.Features.Discovery;
using MinApiLib.Oidc.Server.Features.Introspection;
using MinApiLib.Oidc.Server.Features.Nonce;
using MinApiLib.Oidc.Server.Features.Revocation;
using MinApiLib.Oidc.Server.Features.Tokens;
using MinApiLib.Oidc.Server.Features.UserInfo;
using Microsoft.AspNetCore.WebUtilities;

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.Authorize;

public class CustomPromptTests : IDisposable
{
    private const string Category = "Authorize endpoint";

    private readonly OidcServerPipeline _pipeline = new();

    public CustomPromptTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = { "openid", "profile" },
            RedirectUris = { "https://client1/callback" }
        });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.IdentityScopes.Add(new IdentityResources.Profile());
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });

        _pipeline.OnPostConfigureServices += services =>
        {
            var builder = new OidcServerBuilder(services);
            builder.AddPromptRedirect(OidcConstants.PromptModes.Create, "/account/create");
            builder.AddPromptHandler<SignedInUsersSkipRegistration>("register");
        };
        _pipeline.Initialize();
        _pipeline.BrowserClient.AllowAutoRedirect = false;
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private string AuthorizeUrl(string prompt) => _pipeline.CreateAuthorizeUrl(
        clientId: "client1",
        responseType: "code",
        scope: "openid profile",
        redirectUri: "https://client1/callback",
        state: "state",
        nonce: "nonce",
        extra: Parameters.FromObject(new { prompt }));

    [Fact]
    [Trait("Category", Category)]
    public async Task Registered_prompt_redirects_to_its_page_with_a_return_url_without_the_prompt()
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("create"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("/account/create", location.AbsolutePath);

        var returnUrl = QueryHelpers.ParseQuery(location.Query)["returnUrl"].ToString();
        Assert.StartsWith("/connect/authorize", returnUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("prompt=", returnUrl, StringComparison.Ordinal);
        Assert.False(_pipeline.LoginWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Return_url_of_a_custom_prompt_continues_the_flow()
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("create"), TestContext.Current.CancellationToken);
        var returnUrl = QueryHelpers.ParseQuery(response.Headers.Location!.Query)["returnUrl"].ToString();

        // The account page signs the new user in and follows the return URL.
        await _pipeline.LoginAsync("bob");
        var callback = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.BaseUrl + returnUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.StartsWith("https://client1/callback?code=", callback.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Handler_can_let_the_request_continue()
    {
        await _pipeline.LoginAsync("bob");

        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("register"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client1/callback?code=", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Handler_can_send_the_user_to_login()
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("register"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(OidcServerPipeline.LoginPage, response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Custom_prompt_with_none_is_rejected()
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("none create"), TestContext.Current.CancellationToken);

        Assert.StartsWith(OidcServerPipeline.ErrorPage, response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unregistered_prompt_is_rejected()
    {
        var response = await _pipeline.BrowserClient.GetAsync(AuthorizeUrl("unknown"), TestContext.Current.CancellationToken);

        Assert.StartsWith(OidcServerPipeline.ErrorPage, response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Discovery_publishes_the_supported_prompt_values()
    {
        var json = await _pipeline.BackChannelClient.GetStringAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken);

        var values = JsonNode.Parse(json)!["prompt_values_supported"]!.AsArray().Select(v => v!.GetValue<string>());
        Assert.Equivalent(new[] { "none", "login", "consent", "select_account", "create", "register" }, values, strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Built_in_prompt_values_cannot_get_a_handler()
    {
        var builder = new OidcServerBuilder(new ServiceCollection());

        Assert.Throws<ArgumentException>(() => builder.AddPromptRedirect("login", "/somewhere"));
        Assert.Throws<ArgumentException>(() => builder.AddPromptHandler<SignedInUsersSkipRegistration>("two words"));
    }

    private sealed class SignedInUsersSkipRegistration : IAuthorizePromptHandler
    {
        public Task<InteractionResponse> ProcessAsync(ValidatedAuthorizeRequest request, string prompt)
            => Task.FromResult(request.Subject.Identity?.IsAuthenticated == true
                ? new InteractionResponse()
                : new InteractionResponse { IsLogin = true });
    }
}
