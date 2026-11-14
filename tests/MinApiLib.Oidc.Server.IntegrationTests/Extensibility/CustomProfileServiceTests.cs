// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Extensibility;

public class CustomProfileServiceTests : IDisposable
{
    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public CustomProfileServiceTests()
    {
        _mockPipeline.OnPostConfigureServices += svcs =>
        {
            svcs.AddTransient<IProfileService, CustomProfileService>();
        };

        _mockPipeline.Clients.Add(new Client
        {
            ClientId = "code_client",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = false,
            RedirectUris = { "https://client/callback" },
            RequireConsent = false,
            AlwaysIncludeUserClaimsInIdToken = true,
            AllowedScopes = { "openid", "custom_identity" }
        });

        _mockPipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _mockPipeline.IdentityScopes.Add(new IdentityResource("custom_identity", new string[] { "foo" }));

        _mockPipeline.Users.Add(new MinApiLib.Oidc.Server.Test.TestUser
        {
            SubjectId = "bob",
            Username = "bob",
            Password = "password",
        });

        _mockPipeline.Initialize();
    }

    [Fact]
    public async Task custom_profile_should_return_claims_for_code_client()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "code_client",
            responseType: "code",
            scope: "openid custom_identity",
            redirectUri: "https://client/callback",
            state: "state",
            nonce: "nonce");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);

        var tokenResponse = await _mockPipeline.BackChannelClient.PostAsync(OidcServerPipeline.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                    { "grant_type", "authorization_code" },
                    { "code", authorization.Code },
                    { "redirect_uri", "https://client/callback" },
                    { "client_id", "code_client" },
            }), TestContext.Current.CancellationToken);
        var tokenJson = JsonNode.Parse(await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).AsObject();
        var idToken = tokenJson["id_token"].ToString();
        Assert.NotNull(idToken);

        var payload = idToken.Split('.')[1];
        var json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(payload));
        var obj = JsonNode.Parse(json).AsObject();

        Assert.NotNull(obj["foo"]);
        Assert.Equal("bar", obj["foo"].ToString());
    }
}

public class CustomProfileService : IProfileService
{
    public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var claims = new Claim[]
        {
                new Claim("foo", "bar")
        };
        context.AddRequestedClaims(claims);
        return Task.CompletedTask;
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        context.IsActive = true;
        return Task.CompletedTask;
    }
}
