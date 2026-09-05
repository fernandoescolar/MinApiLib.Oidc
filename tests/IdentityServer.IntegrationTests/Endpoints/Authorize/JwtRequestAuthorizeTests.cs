namespace IdentityServer.IntegrationTests.Endpoints.Authorize;

public class JwtRequestAuthorizeTests : IDisposable
{
    private const string Category = "Authorize endpoint with JWT requests";

    private readonly IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly Client _client;

    private readonly string _symmetricJwk = @"{ ""kty"": ""oct"", ""use"": ""sig"", ""kid"": ""1"", ""k"": ""nYA-IFt8xTsdBHe9hunvizcp3Dt7f6qGqudq18kZHNtvqEGjJ9Ud-9x3kbQ-LYfLHS3xM2MpFQFg1JzT_0U_F8DI40oby4TvBDGszP664UgA8_5GjB7Flnrlsap1NlitvNpgQX3lpyTvC2zVuQ-UVsXbBDAaSBUSlnw7SE4LM8Ye2WYZrdCCXL8yAX9vIR7vf77yvNTEcBCI6y4JlvZaqMB4YKVSfygs8XqGGCHjLpE5bvI-A4ESbAUX26cVFvCeDg9pR6HK7BmwPMlO96krgtKZcXEJtUELYPys6-rbwAIdmxJxKxpgRpt0FRv_9fm6YPwG7QivYBX-vRwaodL1TA"", ""alg"": ""HS256""}";
    private readonly RsaSecurityKey _rsaKey;

    public JwtRequestAuthorizeTests()
    {
        IdentityModelEventSource.ShowPII = true;

        _rsaKey = CryptoHelper.CreateRsaSecurityKey();

        _mockPipeline.Clients.AddRange(new Client[]
        {
            _client = new Client
            {
                ClientName = "Client with keys",
                ClientId = "client",
                Enabled = true,
                RequireRequestObject = true,
                RequireClientSecret = false,
                RequirePkce = false,
                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://client/callback" },
                RequestUris = { "https://client_jwt" },

                ClientSecrets =
                {
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                        Value = Convert.ToBase64String(TestCert.Load().Export(X509ContentType.Cert))
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = _symmetricJwk
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
                    }
                },

                AllowedScopes = new List<string>
                {
                    "openid", "profile", "api1", "api2"
                }
            },
            _client = new Client
            {
                ClientName = "Client with keys",
                ClientId = "client2",
                Enabled = true,
                RequireRequestObject = true,
                RequireClientSecret = false,
                RequirePkce = false,
                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://client/callback" },
                RequestUris = { "https://client_jwt" },

                ClientSecrets =
                {
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                        Value = Convert.ToBase64String(TestCert.Load().Export(X509ContentType.Cert))
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = _symmetricJwk
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsaKey))
                    },
                    new Secret
                    {
                        Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                        Value = JsonSerializer.Serialize(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(TestCert.Load())))
                    }
                },

                AllowedScopes = new List<string>
                {
                    "openid", "profile", "api1", "api2"
                }
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

    string CreateRequestJwt(string issuer, string audience, SigningCredentials credential, Claim[] claims, bool setJwtTyp = false)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();

        var token = handler.CreateJwtSecurityToken(
            issuer: issuer,
            audience: audience,
            signingCredentials: credential,
            subject: Identity.Create("pwd", claims));

        if (setJwtTyp)
        {
            token.Header["typ"] = JwtClaimTypes.JwtTypes.AuthorizationRequest;
        }

        return handler.WriteToken(token);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_request_object_should_fail()
    {
        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            scope: "openid profile",
            state: "123state",
            nonce: "123nonce",
            redirectUri: "https://client/callback");
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Client must use request object, but no request or request_uri parameter present", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_X509_certificate()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new { request = requestJwt }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);

        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.Equal(11, _mockPipeline.LoginRequest.RequestObjectValues.Count);
        Assert.Contains("foo", _mockPipeline.LoginRequest.RequestObjectValues);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.RequestObjectValues["foo"]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_symmetric_jwk()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new SigningCredentials(new Microsoft.IdentityModel.Tokens.JsonWebKey(_symmetricJwk), "HS256"),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);

        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.Equal(11, _mockPipeline.LoginRequest.RequestObjectValues.Count);
        Assert.Contains("foo", _mockPipeline.LoginRequest.RequestObjectValues);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.RequestObjectValues["foo"]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_valid_JWT_request_object_parameters_using_rsa_jwk()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new SigningCredentials(_rsaKey, "RS256"),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);

        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.Equal(11, _mockPipeline.LoginRequest.RequestObjectValues.Count);
        Assert.Contains("foo", _mockPipeline.LoginRequest.RequestObjectValues);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.RequestObjectValues["foo"]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task correct_jwt_typ_should_pass_strict_validation()
    {
        _mockPipeline.Options.StrictJarValidation = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new SigningCredentials(_rsaKey, "RS256"),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            }, setJwtTyp: true);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);

        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.Equal(11, _mockPipeline.LoginRequest.RequestObjectValues.Count);
        Assert.Contains("foo", _mockPipeline.LoginRequest.RequestObjectValues);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.RequestObjectValues["foo"]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task missing_jwt_typ_should_error()
    {
        _mockPipeline.Options.StrictJarValidation = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new SigningCredentials(_rsaKey, "RS256"),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task mismatch_in_jwt_values_should_error()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new SigningCredentials(_rsaKey, "RS256"),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            scope: "bad",
            state: "bad",
            nonce: "bad",
            redirectUri: "bad",
            acrValues: "bad",
            loginHint: "bad",
            extra: Parameters.FromObject(new
            {
                display = "bad",
                ui_locales = "bad",
                foo = "bad",
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Parameter mismatch in JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_complex_objects_in_request_object()
    {
        var someObj = new { foo = new { bar = "bar" }, baz = "baz" };
        var someObjJson = JsonSerializer.Serialize(someObj);
        var someArr = new[] { "a", "b", "c" };
        var someArrJson = JsonSerializer.Serialize(someArr);

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
                new Claim("someObj", someObjJson),
                new Claim("someArr", someArrJson),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);

        Assert.NotNull(_mockPipeline.LoginRequest.Parameters["someObj"]);
        var someObj2 = JsonSerializer.Deserialize(_mockPipeline.LoginRequest.Parameters["someObj"], someObj.GetType());
        Assert.Equivalent(someObj2, someObj, strict: true);
        Assert.NotNull(_mockPipeline.LoginRequest.Parameters["someArr"]);
        var someArr2 = JsonSerializer.Deserialize<string[]>(_mockPipeline.LoginRequest.Parameters["someArr"]);
        Assert.Equivalent(new[] { "a", "c", "b" }, someArr2, strict: true);
        Assert.Equal(3, someArr2.Length);

        Assert.Equal(13, _mockPipeline.LoginRequest.RequestObjectValues.Count);
        Assert.NotNull(_mockPipeline.LoginRequest.RequestObjectValues["someObj"]);
        someObj2 = JsonSerializer.Deserialize(_mockPipeline.LoginRequest.RequestObjectValues["someObj"], someObj.GetType());
        Assert.Equivalent(someObj2, someObj, strict: true);
        Assert.NotNull(_mockPipeline.LoginRequest.RequestObjectValues["someArr"]);
        someArr2 = JsonSerializer.Deserialize<string[]>(_mockPipeline.LoginRequest.Parameters["someArr"]);
        Assert.Equivalent(new[] { "a", "c", "b" }, someArr2, strict: true);
        Assert.Equal(3, someArr2.Length);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_without_client_id()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid client_id", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_without_client_id_in_jwt()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_if_audience_is_incorrect()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: "invalid",
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_if_issuer_does_not_match_client_id()
    {
        var requestJwt = CreateRequestJwt(
            issuer: "invalid",
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_that_includes_request_param()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
                new Claim("request", "request")
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_that_includes_request_uri_param()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
                new Claim("request_uri", "request_uri")
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_object", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_if_response_type_does_not_match()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "id_token token"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo")
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_jwt_request_if_client_id_does_not_match()
    {
        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("response_type", "code"),
                new Claim("client_id", "client"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo")
            });

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: "client2",
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request", _mockPipeline.ErrorMessage.Error);
        Assert.Equal("Invalid JWT request", _mockPipeline.ErrorMessage.ErrorDescription);
        Assert.Null(_mockPipeline.LoginRequest);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_ignore_request_uri_when_feature_is_disabled()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = false;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });
        _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
        {
            Assert.Equal(new Uri("https://client_jwt"), req.RequestUri);
            return Task.CompletedTask;
        };
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_mockPipeline.ErrorWasCalled);

        Assert.False(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_request_uri_with_valid_jwt()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });
        _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
        {
            Assert.Equal(new Uri("https://client_jwt"), req.RequestUri);
            Assert.True(req.Options.TryGetValue(new HttpRequestOptionsKey<Client>(IdentityServerConstants.JwtRequestClientKey),
                out _));
            return Task.CompletedTask;
        };
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);
        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_accept_request_uri_with_valid_jwt_and_strict_validation()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;
        _mockPipeline.Options.StrictJarValidation = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            }, setJwtTyp: true);
        _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
        {
            Assert.Equal(new Uri("https://client_jwt"), req.RequestUri);
            return Task.CompletedTask;
        };
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);
        _mockPipeline.JwtRequestMessageHandler.Response.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/{JwtClaimTypes.JwtTypes.AuthorizationRequest}");

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(_mockPipeline.LoginRequest);
        Assert.Equal(_client.ClientId, _mockPipeline.LoginRequest.Client.ClientId);
        Assert.Equal("popup", _mockPipeline.LoginRequest.DisplayMode);
        Assert.Equal("ui_locale_value", _mockPipeline.LoginRequest.UiLocales);
        Assert.Equal("idp_value", _mockPipeline.LoginRequest.IdP);
        Assert.Equal("tenant_value", _mockPipeline.LoginRequest.Tenant);
        Assert.Equal("login_hint_value", _mockPipeline.LoginRequest.LoginHint);
        Assert.Equivalent(new string[] { "acr_2", "acr_1" }, _mockPipeline.LoginRequest.AcrValues, strict: true);
        Assert.Contains("foo", _mockPipeline.LoginRequest.Parameters.AllKeys);
        Assert.Equal("123foo", _mockPipeline.LoginRequest.Parameters["foo"]);

        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task authorize_should_reject_request_uri_with_valid_jwt_and_strict_validation_but_invalid_content_type()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;
        _mockPipeline.Options.StrictJarValidation = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            }, setJwtTyp: true);
        _mockPipeline.JwtRequestMessageHandler.OnInvoke = req =>
        {
            Assert.Equal(new Uri("https://client_jwt"), req.RequestUri);
            return Task.CompletedTask;
        };
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        var response = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("invalid_request_uri", _mockPipeline.ErrorMessage.Error);
        Assert.Null(_mockPipeline.LoginRequest);
        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task request_uri_response_returns_500_should_fail()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        _mockPipeline.JwtRequestMessageHandler.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.LoginRequest);

        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task request_uri_response_returns_404_should_fail()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        _mockPipeline.JwtRequestMessageHandler.Response = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "https://client_jwt"
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.LoginRequest);

        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task request_uri_length_too_long_should_fail()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request_uri = "http://" + new string('x', 512)
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.LoginRequest);

        Assert.False(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("http://example.com/jwt")]           // plain HTTP not allowed
    [InlineData("ftp://example.com/jwt")]             // non-HTTP scheme not allowed
    [InlineData("https://127.0.0.1/jwt")]             // IPv4 loopback
    [InlineData("https://[::1]/jwt")]                 // IPv6 loopback
    [InlineData("https://[fe80::1]/jwt")]             // IPv6 link-local
    [InlineData("https://10.0.0.1/jwt")]              // RFC 1918 10.0.0.0/8
    [InlineData("https://172.16.0.1/jwt")]            // RFC 1918 172.16.0.0/12
    [InlineData("https://192.168.1.1/jwt")]           // RFC 1918 192.168.0.0/16
    [InlineData("https://169.254.169.254/jwt")]       // cloud metadata endpoint
    [InlineData("https://0.0.0.1/jwt")]               // 0.0.0.0/8 (this network, RFC 1122)
    [InlineData("https://100.64.0.1/jwt")]            // 100.64.0.0/10 (Shared Address Space, RFC 6598)
    [InlineData("https://100.127.255.255/jwt")]       // upper bound of 100.64.0.0/10
    public async Task request_uri_targeting_private_or_non_https_should_be_rejected(string requestUri)
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;
        // Registered on purpose, so the address checks themselves are exercised.
        _client.RequestUris.Add(requestUri);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new { request_uri = requestUri }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.LoginRequest);
        Assert.False(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task both_request_and_request_uri_params_should_fail()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
                new Claim("acr_values", "acr_1 acr_2 tenant:tenant_value idp:idp_value"),
                new Claim("login_hint", "login_hint_value"),
                new Claim("display", "popup"),
                new Claim("ui_locales", "ui_locale_value"),
                new Claim("foo", "123foo"),
            });
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new
            {
                request = requestJwt,
                request_uri = "https://client_jwt"
            }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Null(_mockPipeline.LoginRequest);

        Assert.False(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task unregistered_request_uri_should_be_rejected_without_fetching()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new { request_uri = "https://attacker.example/request.jwt" }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.ErrorWasCalled);
        Assert.Equal(OidcConstants.AuthorizeErrors.InvalidRequestUri, _mockPipeline.ErrorMessage.Error);
        Assert.False(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task registered_request_uri_with_different_fragment_should_be_accepted()
    {
        _mockPipeline.Options.Endpoints.EnableJwtRequestUri = true;

        var requestJwt = CreateRequestJwt(
            issuer: _client.ClientId,
            audience: IdentityServerPipeline.BaseUrl,
            credential: new X509SigningCredentials(TestCert.Load()),
            claims: new[] {
                new Claim("client_id", _client.ClientId),
                new Claim("response_type", "code"),
                new Claim("scope", "openid profile"),
                new Claim("state", "123state"),
                new Claim("nonce", "123nonce"),
                new Claim("redirect_uri", "https://client/callback"),
            });
        _mockPipeline.JwtRequestMessageHandler.Response.Content = new StringContent(requestJwt);

        var url = _mockPipeline.CreateAuthorizeUrl(
            clientId: _client.ClientId,
            responseType: "code",
            extra: Parameters.FromObject(new { request_uri = "https://client_jwt#sha256-abc" }));
        _ = await _mockPipeline.BrowserClient.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(_mockPipeline.JwtRequestMessageHandler.InvokeWasCalled);
        Assert.NotNull(_mockPipeline.LoginRequest);
    }
}
