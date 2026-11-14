// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Conformance.Basic;

public class ClientAuthenticationTests : IDisposable
{
    private const string Category = "Conformance.Basic.ClientAuthenticationTests";

    private OidcServerPipeline _pipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public ClientAuthenticationTests()
    {
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.Clients.Add(new Client
        {
            Enabled = true,
            ClientId = "code_pipeline.Client",
            ClientSecrets = new List<Secret>
                {
                    new Secret("secret".Sha512())
                },

            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = { "openid" },

            RequireConsent = false,
            RequirePkce = false,
            RedirectUris = new List<string>
                {
                    "https://code_pipeline.Client/callback",
                    "https://code_pipeline.Client/callback?foo=bar&baz=quux"
                }
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
    [Trait("Category", Category)]
    public async Task Token_endpoint_supports_client_authentication_with_basic_authentication_with_POST()
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _pipeline.CreateAuthorizeUrl(
                       clientId: "code_pipeline.Client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://code_pipeline.Client/callback?foo=bar&baz=quux",
                       nonce: nonce);
        var response = await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        var authorization = _pipeline.ParseAuthorizationResponseUrl(response.Headers.Location.ToString());
        Assert.NotNull(authorization.Code);

        var code = authorization.Code;

        var wrapper = new MessageHandlerWrapper(_pipeline.Handler);
        var tokenClient = new HttpClient(wrapper);
        var tokenResult = await tokenClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = "code_pipeline.Client",
            ClientSecret = "secret",

            Code = code,
            RedirectUri = "https://code_pipeline.Client/callback?foo=bar&baz=quux"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResult.IsError);
        Assert.Equal("OK", tokenResult.HttpErrorReason);
        Assert.Equal("Bearer", tokenResult.TokenType);
        Assert.NotNull(tokenResult.AccessToken);
        Assert.True(tokenResult.ExpiresIn > 0);
        Assert.NotNull(tokenResult.IdentityToken);

        Assert.True(wrapper.Response.Headers.CacheControl.NoCache);
        Assert.True(wrapper.Response.Headers.CacheControl.NoStore);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Token_endpoint_supports_client_authentication_with_form_encoded_authentication_in_POST_body()
    {
        await _pipeline.LoginAsync("bob");

        var nonce = Guid.NewGuid().ToString();

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var url = _pipeline.CreateAuthorizeUrl(
                       clientId: "code_pipeline.Client",
                       responseType: "code",
                       scope: "openid",
                       redirectUri: "https://code_pipeline.Client/callback?foo=bar&baz=quux",
                       nonce: nonce);
        var response = await _pipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        var authorization = _pipeline.ParseAuthorizationResponseUrl(response.Headers.Location.ToString());
        Assert.NotNull(authorization.Code);

        var code = authorization.Code;

        var wrapper = new MessageHandlerWrapper(_pipeline.Handler);
        var tokenClient = new HttpClient(wrapper);
        var tokenResult = await tokenClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = OidcServerPipeline.TokenEndpoint,
            ClientId = "code_pipeline.Client",
            ClientSecret = "secret",
            ClientCredentialStyle = ClientCredentialStyle.PostBody,

            Code = code,
            RedirectUri = "https://code_pipeline.Client/callback?foo=bar&baz=quux"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(tokenResult.IsError);
        Assert.Equal("OK", tokenResult.HttpErrorReason);
        Assert.Equal("Bearer", tokenResult.TokenType);
        Assert.NotNull(tokenResult.AccessToken);
        Assert.True(tokenResult.ExpiresIn > 0);
        Assert.NotNull(tokenResult.IdentityToken);
    }
}
