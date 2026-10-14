namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.Authorize;

/// <summary>
/// <see cref="Core.Configuration.AuthenticationOptions.EnforceAcrValues"/>: the session's <c>acr</c> must be one of the
/// requested <c>acr_values</c>, after one chance to sign in again.
/// </summary>
public sealed class AcrValuesTests : IDisposable
{
    private const string Category = "Authorize endpoint";

    private readonly OidcServerPipeline _pipeline = new();

    public AcrValuesTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = "client",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = { "openid" },
            RedirectUris = { "https://client/callback" }
        });
        _pipeline.Users.Add(new TestUser { SubjectId = "bob", Username = "bob" });
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.Initialize();
        _pipeline.Options.Authentication.EnforceAcrValues = true;
        _pipeline.BrowserClient.AllowAutoRedirect = false;
    }

    public void Dispose() => _pipeline.Dispose();

    private static ClaimsPrincipal Bob(string acr = null)
    {
        var user = new OidcServerUser("bob");
        if (acr != null)
        {
            user.AdditionalClaims.Add(new Claim(JwtClaimTypes.AuthenticationContextClassReference, acr));
        }

        return user.CreatePrincipal();
    }

    private Task<HttpResponseMessage> AuthorizeAsync(string acrValues, string prompt = null)
        => _pipeline.BrowserClient.GetAsync(_pipeline.CreateAuthorizeUrl(
            clientId: "client",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client/callback",
            state: "state",
            nonce: "nonce",
            acrValues: acrValues,
            extra: prompt == null ? null : Parameters.FromObject(new { prompt })), TestContext.Current.CancellationToken);

    private static ProtocolClient.AuthorizeResponse ClientResponse(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client/callback", response.Headers.Location.ToString());
        return new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Session_with_a_requested_acr_gets_a_code()
    {
        await _pipeline.LoginAsync(Bob("phr"));
        _pipeline.LoginWasCalled = false;

        var response = ClientResponse(await AuthorizeAsync("mfa phr"));

        Assert.False(response.IsError);
        Assert.NotNull(response.Code);
        Assert.False(_pipeline.LoginWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Session_without_the_requested_acr_is_sent_to_the_login_page_with_the_values()
    {
        await _pipeline.LoginAsync(Bob());
        _pipeline.BrowserClient.AllowAutoRedirect = true;

        await AuthorizeAsync("phr");

        Assert.True(_pipeline.LoginWasCalled);
        Assert.Equal(["phr"], _pipeline.LoginRequest.AcrValues);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Stronger_sign_in_at_the_login_page_gets_a_code()
    {
        await _pipeline.LoginAsync(Bob("pwd"));
        _pipeline.Subject = Bob("phr");
        _pipeline.BrowserClient.AllowAutoRedirect = true;
        _pipeline.BrowserClient.StopRedirectingAfter = 2;

        var response = ClientResponse(await AuthorizeAsync("phr"));

        Assert.True(_pipeline.LoginWasCalled);
        Assert.False(response.IsError);
        Assert.NotNull(response.Code);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Sign_in_that_still_does_not_meet_the_values_fails_without_a_second_login()
    {
        await _pipeline.LoginAsync(Bob("pwd"));
        _pipeline.Subject = Bob("pwd");
        _pipeline.BrowserClient.AllowAutoRedirect = true;
        _pipeline.BrowserClient.StopRedirectingAfter = 2;

        var response = ClientResponse(await AuthorizeAsync("phr"));

        Assert.True(response.IsError);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Anonymous_user_who_signs_in_without_the_values_gets_the_error()
    {
        _pipeline.Subject = Bob();
        _pipeline.BrowserClient.AllowAutoRedirect = true;
        _pipeline.BrowserClient.StopRedirectingAfter = 2;

        var response = ClientResponse(await AuthorizeAsync("phr"));

        Assert.Equal(OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Anonymous_user_who_signs_in_with_the_values_gets_a_code()
    {
        _pipeline.Subject = Bob("phr");
        _pipeline.BrowserClient.AllowAutoRedirect = true;
        _pipeline.BrowserClient.StopRedirectingAfter = 2;

        var response = ClientResponse(await AuthorizeAsync("phr"));

        Assert.NotNull(response.Code);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Prompt_none_without_the_values_gets_login_required()
    {
        await _pipeline.LoginAsync(Bob());

        var response = ClientResponse(await AuthorizeAsync("phr", prompt: "none"));

        Assert.Equal(OidcConstants.AuthorizeErrors.LoginRequired, response.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Idp_and_tenant_values_are_not_required()
    {
        await _pipeline.LoginAsync(Bob());

        var response = ClientResponse(await AuthorizeAsync("idp:local tenant:contoso"));

        Assert.NotNull(response.Code);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Without_the_option_acr_values_are_voluntary()
    {
        _pipeline.Options.Authentication.EnforceAcrValues = false;
        await _pipeline.LoginAsync(Bob());
        _pipeline.LoginWasCalled = false;

        var response = ClientResponse(await AuthorizeAsync("phr"));

        Assert.NotNull(response.Code);
        Assert.False(_pipeline.LoginWasCalled);
    }
}
