// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Pipeline;

public class SubpathHosting : IDisposable
{
    private const string Category = "Subpath endpoint";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    private Client _client1;

    public SubpathHosting()
    {
        _mockPipeline.Clients.AddRange(new Client[] {
                _client1 = new Client
                {
                    ClientId = "client1",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequirePkce = false,
                    RequireConsent = false,
                    AllowedScopes = new List<string> { "openid", "profile" },
                    RedirectUris = new List<string> { "https://client1/callback" },
                }
            });

        _mockPipeline.Users.Add(new TestUser
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

        _mockPipeline.IdentityScopes.AddRange(new IdentityResource[] {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            });

        _mockPipeline.Initialize("/subpath");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task anonymous_user_should_be_redirected_to_login_page()
    {
        var url = new RequestUrl("https://server/subpath/connect/authorize").CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
    }
}
