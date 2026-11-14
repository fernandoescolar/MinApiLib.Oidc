// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Pipeline;

public class FederatedSignoutTests : IDisposable
{
    private OidcServerPipeline _pipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private ClaimsPrincipal _user;

    public FederatedSignoutTests()
    {
        _user = new OidcServerUser("bob")
        {
            AdditionalClaims = { new Claim(JwtClaimTypes.SessionId, "123") }
        }.CreatePrincipal();

        _pipeline = new OidcServerPipeline();

        _pipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId()
            });

        _pipeline.Clients.Add(new Client
        {
            ClientId = "client1",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RequireConsent = false,
            AllowedScopes = new List<string> { "openid" },
            RedirectUris = new List<string> { "https://client1/callback" },
            FrontChannelLogoutUri = "https://client1/signout",
            PostLogoutRedirectUris = new List<string> { "https://client1/signout-callback" },
        });

        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Claims = new Claim[]
           {
                    new Claim("name", "Bob Loblaw"),
                    new Claim("email", "bob@loblaw.com"),
                    new Claim("role", "Attorney")
           }
        });

        _pipeline.Initialize();
    }

    [Fact]
    public async Task valid_request_to_federated_signout_endpoint_should_render_page_with_iframe()
    {
        await _pipeline.LoginAsync(_user);

        await _pipeline.RequestAuthorizationEndpointAsync(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        var response = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.FederatedSignOutUrl + "?sid=123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("https://server/connect/endsession/callback?endSessionId=", html);
    }

    [Fact]
    public async Task valid_POST_request_to_federated_signout_endpoint_should_render_page_with_iframe()
    {
        await _pipeline.LoginAsync(_user);

        await _pipeline.RequestAuthorizationEndpointAsync(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        var response = await _pipeline.BrowserClient.PostAsync(OidcServerPipeline.FederatedSignOutUrl, new FormUrlEncodedContent(new Dictionary<string, string> { { "sid", "123" } }), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("https://server/connect/endsession/callback?endSessionId=", html);
    }

    [Fact]
    public async Task no_clients_signed_into_should_not_render_page_with_iframe()
    {
        await _pipeline.LoginAsync(_user);

        var response = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.FederatedSignOutUrl + "?sid=123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(String.Empty, html);
    }

    [Fact]
    public async Task no_authenticated_user_should_not_render_page_with_iframe()
    {
        var response = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.FederatedSignOutUrl + "?sid=123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(String.Empty, html);
    }

    [Fact]
    public async Task user_not_signed_out_should_not_render_page_with_iframe()
    {
        _pipeline.OnFederatedSignout = ctx =>
        {
            return Task.FromResult(true);
        };

        await _pipeline.LoginAsync(_user);

        await _pipeline.RequestAuthorizationEndpointAsync(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        var response = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.FederatedSignOutUrl + "?sid=123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(String.Empty, html);
    }

    [Fact]
    public async Task non_200_should_not_render_page_with_iframe()
    {
        _pipeline.OnFederatedSignout = async ctx =>
        {
            await ctx.SignOutAsync();
            ctx.Response.Redirect("http://foo");
            return true;
        };

        await _pipeline.LoginAsync(_user);

        await _pipeline.RequestAuthorizationEndpointAsync(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _pipeline.BrowserClient.GetAsync(OidcServerPipeline.FederatedSignOutUrl + "?sid=123", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        var html = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(String.Empty, html);
    }
}
