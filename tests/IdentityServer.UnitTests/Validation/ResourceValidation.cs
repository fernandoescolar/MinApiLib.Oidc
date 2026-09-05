namespace IdentityServer.UnitTests.Validation;

public class ResourceValidation
{
    private const string Category = "Resource Validation";

    private List<IdentityResource> _identityResources = new List<IdentityResource>
    {
        new IdentityResource
        {
            Name = "openid",
            Required = true
        },
        new IdentityResource
        {
            Name = "email"
        }
    };

    private List<ApiResource> _apiResources = new List<ApiResource>
    {
        new ApiResource
        {
            Name = "api",
            Scopes = { "resource1", "resource2" }
        },
        new ApiResource
        {
            Name = "disabled_api",
            Enabled = false,
            Scopes = { "disabled" }
        }
    };

    private List<ApiScope> _scopes = new List<ApiScope> {
        new ApiScope
        {
            Name = "resource1",
            Required = true
        },
        new ApiScope
        {
            Name = "resource2"
        }
    };

    private Client _restrictedClient = new Client
    {
        ClientId = "restricted",

        AllowedScopes = new List<string>
        {
            "openid",
            "resource1",
            "disabled"
        }
    };

    private IResourceStore _store;

    public ResourceValidation()
    {
        _store = new InMemoryResourcesStore(_identityResources, _apiResources, _scopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Parse_Scopes_with_Empty_Scope_List()
    {
        var scopes = string.Empty.ParseScopesString();

        Assert.Null(scopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Parse_Scopes_with_Sorting()
    {
        var scopes = "scope3 scope2 scope1".ParseScopesString();

        Assert.Equal(3, scopes.Count);

        Assert.Equal("scope1", scopes[0]);
        Assert.Equal("scope2", scopes[1]);
        Assert.Equal("scope3", scopes[2]);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Parse_Scopes_with_Extra_Spaces()
    {
        var scopes = "   scope3     scope2     scope1   ".ParseScopesString();

        Assert.Equal(3, scopes.Count);

        Assert.Equal("scope1", scopes[0]);
        Assert.Equal("scope2", scopes[1]);
        Assert.Equal("scope3", scopes[2]);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Parse_Scopes_with_Duplicate_Scope()
    {
        var scopes = "scope2 scope1 scope2".ParseScopesString();

        Assert.Equal(2, scopes.Count);

        Assert.Equal("scope1", scopes[0]);
        Assert.Equal("scope2", scopes[1]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Only_Offline_Access_Requested()
    {
        var scopes = "offline_access".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.False(result.Succeeded);
        Assert.Contains("offline_access", result.InvalidScopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task All_Scopes_Valid()
    {
        var scopes = "openid resource1".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.True(result.Succeeded);
        Assert.Empty(result.InvalidScopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Invalid_Scope()
    {
        {
            var scopes = "openid email resource1 unknown".ParseScopesString();

            var validator = Factory.CreateResourceValidator(_store);
            var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
            {
                Client = _restrictedClient,
                Scopes = scopes
            });

            Assert.False(result.Succeeded);
            Assert.Contains("unknown", result.InvalidScopes);
            Assert.Contains("email", result.InvalidScopes);
        }
        {
            var scopes = "openid resource1 resource2".ParseScopesString();

            var validator = Factory.CreateResourceValidator(_store);
            var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
            {
                Client = _restrictedClient,
                Scopes = scopes
            });

            Assert.False(result.Succeeded);
            Assert.Contains("resource2", result.InvalidScopes);
        }
        {
            var scopes = "openid email resource1".ParseScopesString();

            var validator = Factory.CreateResourceValidator(_store);
            var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
            {
                Client = _restrictedClient,
                Scopes = scopes
            });

            Assert.False(result.Succeeded);
            Assert.Contains("email", result.InvalidScopes);
        }
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Disabled_Scope()
    {
        var scopes = "openid resource1 disabled".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.False(result.Succeeded);
        Assert.Contains("disabled", result.InvalidScopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task All_Scopes_Allowed_For_Restricted_Client()
    {
        var scopes = "openid resource1".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.True(result.Succeeded);
        Assert.Empty(result.InvalidScopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Restricted_Scopes()
    {
        var scopes = "openid email resource1 resource2".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.False(result.Succeeded);
        Assert.Contains("email", result.InvalidScopes);
        Assert.Contains("resource2", result.InvalidScopes);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Contains_Resource_and_Identity_Scopes()
    {
        var scopes = "openid resource1".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.True(result.Succeeded);
        Assert.Contains("openid", result.Resources.IdentityResources.Select(x => x.Name));
        Assert.Contains("resource1", result.Resources.ApiScopes.Select(x => x.Name));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Contains_Resource_Scopes_Only()
    {
        var scopes = "resource1".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.True(result.Succeeded);
        Assert.Empty(result.Resources.IdentityResources);
        Assert.Contains("resource1", result.Resources.ApiScopes.Select(x => x.Name));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Contains_Identity_Scopes_Only()
    {
        var scopes = "openid".ParseScopesString();

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = _restrictedClient,
            Scopes = scopes
        });

        Assert.True(result.Succeeded);
        Assert.Contains("openid", result.Resources.IdentityResources.Select(x => x.Name));
        Assert.Empty(result.Resources.ApiResources);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Scope_matches_multipls_apis_should_succeed()
    {
        _apiResources.Clear();
        _apiResources.Add(new ApiResource { Name = "api1", Scopes = { "resource" } });
        _apiResources.Add(new ApiResource { Name = "api2", Scopes = { "resource" } });
        _scopes.Clear();
        _scopes.Add(new ApiScope("resource"));

        var validator = Factory.CreateResourceValidator(_store);
        var result = await validator.ValidateRequestedResourcesAsync(new IdentityServer.Validation.ResourceValidationRequest
        {
            Client = new Client { AllowedScopes = { "resource" } },
            Scopes = new[] { "resource" }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Resources.ApiResources.Count);
        Assert.Equivalent(new[] { "api1", "api2" }, result.Resources.ApiResources.Select(x => x.Name), strict: true);
        Assert.Single(result.RawScopeValues);
        Assert.Equivalent(new[] { "resource" }, result.RawScopeValues, strict: true);
    }
}
