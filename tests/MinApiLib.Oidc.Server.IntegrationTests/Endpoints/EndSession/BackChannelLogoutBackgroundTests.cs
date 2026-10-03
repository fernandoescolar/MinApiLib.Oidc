namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.EndSession;

public class BackChannelLogoutBackgroundTests : IDisposable
{
    private readonly OidcServerPipeline _pipeline = new() { BackChannelLogoutInBackground = true };

    public BackChannelLogoutBackgroundTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client3",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = { "openid" },
            RedirectUris = { "https://client3/callback" },
            BackChannelLogoutUri = "https://client3/signout"
        });
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.Initialize();
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SignInToClient3AndSignOutAsync()
    {
        await _pipeline.LoginAsync("bob");
        await _pipeline.RequestAuthorizationEndpointAsync(
            clientId: "client3",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce");

        await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.EndSessionEndpoint, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Notification_is_sent_in_the_background_with_the_original_issuer()
    {
        // Completed at the end of the callback: InvokeWasCalled is set before the callback runs.
        var issuer = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pipeline.BackChannelMessageHandler.OnInvoke = async request =>
        {
            var form = await request.Content.ReadAsStringAsync();
            var token = Uri.UnescapeDataString(form.Split('=')[1]);
            issuer.TrySetResult(new JwtSecurityTokenHandler().ReadJwtToken(token).Issuer);
        };

        await SignInToClient3AndSignOutAsync();

        var completed = await Task.WhenAny(issuer.Task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(issuer.Task, completed);
        Assert.Equal("https://server", (await issuer.Task));
    }

    [Fact]
    public async Task Sign_out_response_does_not_wait_for_the_client()
    {
        var release = new TaskCompletionSource();
        _pipeline.BackChannelMessageHandler.OnInvoke = _ => release.Task;

        var signOut = SignInToClient3AndSignOutAsync();
        var completed = await Task.WhenAny(signOut, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        release.SetResult();

        Assert.Same(signOut, completed);
        await signOut;
    }
}
