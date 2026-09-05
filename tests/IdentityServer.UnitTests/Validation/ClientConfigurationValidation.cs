namespace IdentityServer.UnitTests.Validation;

public class ClientConfigurationValidation
{
    private const string Category = "Client Configuration Validation Tests";
    private IClientConfigurationValidator _validator;
    IdentityServerOptions _options;

    public ClientConfigurationValidation()
    {
        _options = new IdentityServerOptions();
        _validator = new DefaultClientConfigurationValidator(_options);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Standard_clients_should_succeed()
    {
        foreach (var client in TestClients.Get())
        {
            var context = await ValidateAsync(client);

            if (!context.IsValid)
            {
                throw new System.Exception($"client {client.ClientId} failed configuration validation: {context.ErrorMessage}");
            }

        }
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Invalid_access_token_lifetime_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },

            AccessTokenLifetime = 0
        };

        return ShouldFailAsync(client, "access token lifetime is 0 or negative");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Invalid_identity_token_lifetime_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },

            IdentityTokenLifetime = 0
        };

        return ShouldFailAsync(client, "identity token lifetime is 0 or negative");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Invalid_absolute_refresh_token_lifetime_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },

            AbsoluteRefreshTokenLifetime = -1
        };

        return ShouldFailAsync(client, "absolute refresh token lifetime is negative");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Invalid_sliding_refresh_token_lifetime_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },

            SlidingRefreshTokenLifetime = -1
        };

        return ShouldFailAsync(client, "sliding refresh token lifetime is negative");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Missing_allowed_grant_type_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            RequireClientSecret = false,
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "no allowed grant type specified");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Missing_client_secret_for_client_credentials_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "Client secret is required for client_credentials, but no client secret is configured.");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Client_credentials_without_required_client_secret_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "Client secret is required for client_credentials, but RequireClientSecret is false.");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Combined_grants_including_client_credentials_without_required_client_secret_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
            RequireClientSecret = false,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "Client secret is required for client_credentials, but RequireClientSecret is false.");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Missing_client_secret_for_hybrid_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Hybrid,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "Client secret is required for hybrid, but no client secret is configured.");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Missing_client_secret_for_code_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "foo" },
        };

        return ShouldFailAsync(client, "Client secret is required for authorization_code, but no client secret is configured.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Not_required_client_secret_for_hybrid_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Hybrid,
            RequireClientSecret = false,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "foo" },
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task empty_grant_types_collection_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = { },
            AllowedScopes = { "foo" },
            RedirectUris = { "https://foo" }
        };

        var context = await ValidateAsync(client);
        await ShouldFailAsync(client, "no allowed grant type specified");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task null_redirect_uris_collection_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            RedirectUris = null,
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task null_post_logout_redirect_uris_collection_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            PostLogoutRedirectUris = null
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task null_redirect_uris_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            RedirectUris = { null }
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task null_post_logout_redirect_uris_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            PostLogoutRedirectUris = { null }
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task empty_redirect_uris_collection_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            RedirectUris = { },
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }
    [Fact]
    [Trait("Category", Category)]
    public async Task empty_post_logout_redirect_uris_collection_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("hash") },
            AllowedScopes = { "foo" },
            PostLogoutRedirectUris = { },
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ValidateUriSchemesAsync_for_invalid_redirecturi_scheme_should_fail()
    {
        _options.Validation.InvalidRedirectUriPrefixes.Add("custom");
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = { "foo" },
            RedirectUris = { "http://callback", "custom://callback" }
        };

        var result = await ValidateAsync(client);
        await ShouldFailAsync(client, "RedirectUri 'custom://callback' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ValidateUriSchemesAsync_for_null_redirecturi_scheme_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },
            RedirectUris = null
        };

        var result = await ValidateAsync(client);
        Assert.True(result.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ValidateUriSchemesAsync_for_valid_redirect_uri_scheme_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },
            RedirectUris = { "http://callback", "custom://callback" }
        };

        var result = await ValidateAsync(client);
        Assert.True(result.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ValidateUriSchemesAsync_for_invalid_post_logout_redirect_uri_scheme_should_fail()
    {
        _options.Validation.InvalidRedirectUriPrefixes.Add("custom");
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            AllowedScopes = { "foo" },
            RedirectUris = { "http://callback" },
            PostLogoutRedirectUris = { "http://postcallback", "custom://postcallback" }
        };

        var result = await ValidateAsync(client);
        await ShouldFailAsync(client, "PostLogoutRedirectUri 'custom://postcallback' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task ValidateUriSchemesAsync_for_valid_post_logout_redirect_uri_scheme_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            AllowedScopes = { "foo" },
            RedirectUris = { "http://callback" },
            PostLogoutRedirectUris = { "http://postcallback", "custom://postcallback" }
        };

        var result = await ValidateAsync(client);
        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("bad")]
    [InlineData("urn:foo")]
    [InlineData("urn:foo:123")]
    [InlineData("http://foo/")]
    [InlineData("http://foo:80/path")]
    [InlineData("http://foo/path")]
    [InlineData("http://foo:123/path")]
    [InlineData("https://foo:443/path")]
    [InlineData("custom://foo/")]
    [InlineData("custom://foo/path")]
    [InlineData("custom://foo:443/")]
    [InlineData("custom://foo:443/path")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData((string)null)]
    public async Task ValidateAllowedCorsOriginsAsync_should_report_invalid_URL_format(string origin)
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RedirectUris = { "http://client" },
            AllowedCorsOrigins = { origin }
        };

        var result = await ValidateAsync(client);
        Assert.False(result.IsValid);
        Assert.Contains("invalid origin", result.ErrorMessage);
        if (!String.IsNullOrWhiteSpace(origin))
        {
            Assert.Contains(origin, result.ErrorMessage);
        }
        else
        {
            Assert.Contains("empty value", result.ErrorMessage);
        }
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("http://foo")]
    [InlineData("http://foo:80")]
    [InlineData("https://foo")]
    [InlineData("http://foo:123")]
    [InlineData("https://foo:456")]
    [InlineData("https://foo:443")]
    [InlineData("custom://foo")]
    [InlineData("custom://foo:443")]
    public async Task ValidateAllowedCorsOriginsAsync_should_allow_valid_formats(string origin)
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RedirectUris = { "http://client" },
            AllowedCorsOrigins = { origin }
        };

        var result = await ValidateAsync(client);
        Assert.True(result.IsValid);
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Implicit_grant_without_AllowImplicitFlow_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Implicit,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "openid" }
        };

        return ShouldFailAsync(client, "The implicit grant is deprecated: set AllowImplicitFlow to enable it for this client.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Implicit_grant_with_AllowImplicitFlow_needs_no_secret()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Implicit,
            AllowImplicitFlow = true,
            RedirectUris = { "https://foo" },
            AllowedScopes = { "openid" }
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid, context.ErrorMessage);
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Implicit_grant_without_redirect_uri_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            AllowedGrantTypes = GrantTypes.Implicit,
            AllowImplicitFlow = true,
            AllowedScopes = { "openid" }
        };

        return ShouldFailAsync(client, "No redirect URI configured.");
    }

    [Fact]
    [Trait("Category", Category)]
    public Task Password_grant_without_AllowResourceOwnerPasswordFlow_should_fail()
    {
        var client = new Client
        {
            ClientId = "id",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            AllowedScopes = { "api" }
        };

        return ShouldFailAsync(client, "The password grant is deprecated: set AllowResourceOwnerPasswordFlow to enable it for this client.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Password_grant_with_AllowResourceOwnerPasswordFlow_should_succeed()
    {
        var client = new Client
        {
            ClientId = "id",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            AllowResourceOwnerPasswordFlow = true,
            AllowedScopes = { "api" }
        };

        var context = await ValidateAsync(client);
        Assert.True(context.IsValid, context.ErrorMessage);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Implicit_cannot_be_combined_with_code_or_hybrid()
    {
        Assert.Throws<InvalidOperationException>(() => new Client { AllowedGrantTypes = { GrantType.Implicit, GrantType.AuthorizationCode } });
        Assert.Throws<InvalidOperationException>(() => new Client { AllowedGrantTypes = { GrantType.Implicit, GrantType.Hybrid } });
    }

    private async Task<ClientConfigurationValidationContext> ValidateAsync(Client client)
    {
        var context = new ClientConfigurationValidationContext(client);
        await _validator.ValidateAsync(context);

        return context;
    }

    private async Task ShouldFailAsync(Client client, string expectedError)
    {
        var context = await ValidateAsync(client);

        Assert.False(context.IsValid);
        Assert.Equal(expectedError, context.ErrorMessage);
    }
}
