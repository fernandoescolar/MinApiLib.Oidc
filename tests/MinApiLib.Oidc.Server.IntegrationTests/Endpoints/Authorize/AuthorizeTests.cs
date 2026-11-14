// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.Authorize;

public class AuthorizeTests : IDisposable
{
    private const string Category = "Authorize endpoint";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    private Client _client1;

    public AuthorizeTests()
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
            },
            new Client
            {
                ClientId = "client2",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = false,
                RequireConsent = true,

                AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                RedirectUris = new List<string> { "https://client2/callback" },
            },
            new Client
            {
                ClientId = "client3",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequirePkce = false,
                RequireConsent = false,

                AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                RedirectUris = new List<string> { "https://client3/callback" },
                EnableLocalLogin = false,
                IdentityProviderRestrictions = new List<string> { "google" }
            },
            new Client
            {
                ClientId = "client4",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RequireConsent = false,
                RequirePkce = false,
                AllowedScopes = new List<string> { "openid", "profile", "api1", "api2" },
                RedirectUris = new List<string> { "https://client4/callback" },
            },

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
        _mockPipeline.ApiResources.AddRange(new ApiResource[] {
            new ApiResource
            {
                Name = "api",
                Scopes = { "api1", "api2" }
            }
        });
        _mockPipeline.ApiScopes.AddRange(new ApiScope[] {
            new ApiScope
            {
                Name = "api1"
            },
            new ApiScope
            {
                Name = "api2"
            }
        });

        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_not_return_404()
    {
        var response = await _mockPipeline.BrowserClient.GetAsync(OidcServerPipeline.AuthorizeEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task post_request_without_form_should_return_415()
    {
        var response = await _mockPipeline.BrowserClient.PostAsync(OidcServerPipeline.AuthorizeEndpoint, new StringContent("foo"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task post_request_should_return_200()
    {
        var response = await _mockPipeline.BrowserClient.PostAsync(OidcServerPipeline.AuthorizeEndpoint,
            new FormUrlEncodedContent(
                new Dictionary<string, string> { }), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_not_return_500()
    {
        var response = await _mockPipeline.BrowserClient.GetAsync(OidcServerPipeline.AuthorizeEndpoint, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(((int)response.StatusCode) < 500);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task anonymous_user_should_be_redirected_to_login_page()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
    }

    [Theory]
    [InlineData((Type)null)]
    [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
    [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
    [Trait("Category", Category)]
    public async Task signin_request_should_have_authorization_params(Type storeType)
    {
        if (storeType != null)
        {
            _mockPipeline.OnPostConfigureServices += services =>
            {
                services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
            };
            _mockPipeline.Initialize();
        }

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            loginHint: "login_hint_value",
            acrValues: "acr_1 acr_2 tenant:tenant_value idp:idp_value",
            extra: Parameters.FromObject(new
            {
                display = "popup",
                ui_locales = "ui_locale_value",
                custom_foo = "foo_value"
            }));
        var response = await _mockPipeline.BrowserClient.GetAsync(url + "&foo=bar", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal("client1", _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);
        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("bar", _mockPipeline.LoginRequest.Parameters["foo"]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task signin_response_should_allow_successful_authorization_response()
    {
        _mockPipeline.Subject = new OidcServerUser("bob").CreatePrincipal();
        _mockPipeline.BrowserClient.StopRedirectingAfter = 2;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client1/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);
        Assert.Equal("123_state", authorization.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authenticated_user_with_valid_request_should_receive_authorization_response()
    {
        await _mockPipeline.LoginAsync("bob");

        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client1/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);
        Assert.Equal("123_state", authorization.State);
    }

    [Theory]
    [InlineData((Type)null)]
    [InlineData(typeof(QueryStringAuthorizationParametersMessageStore))]
    [InlineData(typeof(DistributedCacheAuthorizationParametersMessageStore))]
    [Trait("Category", Category)]
    public async Task login_response_and_consent_response_should_receive_authorization_response(Type storeType)
    {
        if (storeType != null)
        {
            _mockPipeline.OnPostConfigureServices += services =>
            {
                services.AddTransient(typeof(IAuthorizationParametersMessageStore), storeType);
            };
            _mockPipeline.Initialize();
        }

        _mockPipeline.Subject = new OidcServerUser("bob").CreatePrincipal();

        _mockPipeline.ConsentResponse = new ConsentResponse()
        {
            ScopesValuesConsented = new string[] { "openid", "api1", "profile" }
        };

        _mockPipeline.BrowserClient.StopRedirectingAfter = 4;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            scope: "openid profile api1 api2",
            redirectUri: "https://client2/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://client2/callback", response.Headers.Location.ToString());

        var authorization = new ProtocolClient.AuthorizeResponse(response.Headers.Location.ToString());
        Assert.False(authorization.IsError);
        Assert.NotNull(authorization.Code);
        Assert.Equal("123_state", authorization.State);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task idp_should_be_passed_to_login_page()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client3",
            responseType: "code",
            scope: "openid profile",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "idp:google");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
        Assert.Equal("google", _mockPipeline.LoginRequest.IdP);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task idp_not_allowed_by_client_should_not_be_passed_to_login_page()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client3",
            responseType: "code",
            scope: "openid profile",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: "idp:facebook");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
        Assert.Null(_mockPipeline.LoginRequest.IdP);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task user_idp_not_allowed_by_client_should_cause_login_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client3",
            responseType: "code",
            scope: "openid profile",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
        Assert.Null(_mockPipeline.LoginRequest.IdP);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task for_invalid_client_error_page_should_not_receive_client_id()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: null,
            responseType: "code",
            scope: "openid",
            redirectUri: "https://invalid",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.ClientId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task error_page_should_receive_client_id()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://invalid",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("client1", _mockPipeline.ErrorMessage.ClientId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_redirect_uri_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://invalid",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("redirect_uri", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_redirect_uri_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://invalid",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_client_id_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1_invalid",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_client_id_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1_invalid",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_redirect_uri_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1_invalid",
            responseType: "code",
            scope: "openid", state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("client", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_redirect_uri_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1_invalid",
            responseType: "code",
            scope: "openid", state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task malformed_redirect_uri_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1_invalid",
            responseType: "code",
            scope: "openid",
            redirectUri: "invalid-uri",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("client", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task disabled_client_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        _client1.Enabled = false;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task disabled_client_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        _client1.Enabled = false;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_protocol_for_client_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        _client1.ProtocolType = "invalid";

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnauthorizedClient, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("protocol", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_protocol_for_client_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        _client1.ProtocolType = "invalid";

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_response_type_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "invalid",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnsupportedResponseType, _mockPipeline.ErrorMessage.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_response_type_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "invalid",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_response_mode_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            responseMode: "invalid",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.UnsupportedResponseType, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("response_mode", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_response_mode_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            responseMode: "invalid",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_scope_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code", redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("scope", _mockPipeline.ErrorMessage.ErrorDescription);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_scope_should_not_pass_return_url_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code", redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task explicit_response_mode_should_not_be_passed_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            responseMode: "form_post", redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task scope_too_long_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: new string('x', 500),
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("scope", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_openid_scope_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "profile",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidScope, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("scope", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Contains("openid", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_not_allowed_access_to_scope_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid email",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidScope, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("scope", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }


    [Fact]
    [Trait("Category", Category)]
    public async Task nonce_too_long_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: new string('x', 500));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("nonce", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task locale_too_long_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            extra: Parameters.FromObject(new { ui_locales = new string('x', 500) }));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("ui_locales", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task invalid_max_age_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            extra: Parameters.FromObject(new { max_age = "invalid" }));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("max_age", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task negative_max_age_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            extra: Parameters.FromObject(new { max_age = "-10" }));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("max_age", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task login_hint_too_long_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            loginHint: new string('x', 500));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("login_hint", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task acr_values_too_long_should_show_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: new string('x', 500));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequest, _mockPipeline.ErrorMessage.Error);
        Assert.Contains("acr_values", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.ErrorMessage.RedirectUri);
        Assert.Null(_mockPipeline.ErrorMessage.ResponseMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task overlapping_identity_scopes_and_api_scopes_should_show_error_page()
    {
        _mockPipeline.IdentityScopes.Add(new IdentityResource("foo", "Foo", new string[] { "name" }));
        _mockPipeline.IdentityScopes.Add(new IdentityResource("bar", "Bar", new string[] { "name" }));
        _mockPipeline.ApiScopes.Add(new ApiScope("foo", "Foo"));
        _mockPipeline.ApiScopes.Add(new ApiScope("bar", "Bar"));

        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid foo bar",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");

        var a = () => _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(a);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ui_locales_should_be_passed_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: new string('x', 500),
            extra: Parameters.FromObject(new { ui_locales = "fr-FR" }));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("fr-FR", _mockPipeline.ErrorMessage.UiLocales);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task display_mode_should_be_passed_to_error_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce",
            acrValues: new string('x', 500),
            extra: Parameters.FromObject(new { display = "popup" }));
        await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal("popup", _mockPipeline.ErrorMessage.DisplayMode);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task unicode_values_in_url_should_be_processed_correctly()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        url = url.Replace(OidcServerPipeline.BaseUrl, "https://грант.рф");

        var result = await _mockPipeline.BackChannelClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("xn--80af5akm.xn--p1ai", result.Headers.Location.Authority);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task code_flow_with_fragment_response_type_should_be_allowed()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client4",
            responseType: "code",
            responseMode: "fragment",
            scope: "openid",
            redirectUri: "https://client4/callback",
            state: "123_state",
            nonce: "123_nonce");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_mockPipeline.LoginWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task prompt_login_should_show_login_page()
    {
        await _mockPipeline.LoginAsync("bob");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client3",
            responseType: "code",
            scope: "openid profile",
            redirectUri: "https://client3/callback",
            state: "123_state",
            nonce: "123_nonce",
            extra: Parameters.FromObject(new { prompt = "login" })
        );
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.LoginWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorization_response_should_include_iss()
    {
        await _mockPipeline.LoginAsync("bob");
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client1",
            responseType: "code",
            scope: "openid",
            redirectUri: "https://client1/callback",
            state: "123_state",
            nonce: "123_nonce");
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
        Assert.Equal("https://server", query["iss"].ToString());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task post_authorize_request_should_redirect_with_see_other()
    {
        await _mockPipeline.LoginAsync("bob");
        _mockPipeline.BrowserClient.AllowAutoRedirect = false;

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", "client1" },
            { "response_type", "code" },
            { "scope", "openid" },
            { "redirect_uri", "https://client1/callback" },
            { "state", "123_state" },
            { "nonce", "123_nonce" }
        });
        var response = await _mockPipeline.BrowserClient.PostAsync(OidcServerPipeline.AuthorizeEndpoint, form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.StartsWith("https://client1/callback", response.Headers.Location.ToString());
    }
}
