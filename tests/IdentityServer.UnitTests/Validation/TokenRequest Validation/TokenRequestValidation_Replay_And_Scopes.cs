namespace IdentityServer.UnitTests.Validation.TokenRequest_Validation;

public class TokenRequestValidation_Replay_And_Scopes
{
    private const string Category = "TokenRequest Validation - Replay and scopes";

    private readonly IClientStore _clients = Factory.CreateClientStore();

    [Fact]
    [Trait("Category", Category)]
    public async Task Redeeming_a_code_twice_should_fail_and_revoke_tokens_of_subject_and_client()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var codeStore = Factory.CreateAuthorizationCodeStore();
        var refreshTokenStore = Factory.CreateRefreshTokenStore();
        var referenceTokenStore = Factory.CreateReferenceTokenStore();

        var handle = await codeStore.StoreAuthorizationCodeAsync(CreateCode(client));
        var issuedRefreshToken = await refreshTokenStore.StoreRefreshTokenAsync(new RefreshToken
        {
            CreationTime = DateTime.UtcNow,
            Lifetime = 600,
            AccessToken = new Token { ClientId = client.ClientId, Claims = new List<Claim> { new("sub", "123") } }
        });
        var issuedReferenceToken = await referenceTokenStore.StoreReferenceTokenAsync(new Token
        {
            ClientId = client.ClientId,
            CreationTime = DateTime.UtcNow,
            Lifetime = 600,
            Claims = new List<Claim> { new("sub", "123") }
        });

        var first = await Factory.CreateTokenRequestValidator(authorizationCodeStore: codeStore, refreshTokenStore: refreshTokenStore, referenceTokenStore: referenceTokenStore)
            .ValidateRequestAsync(CodeParameters(handle), client.ToValidationResult());
        Assert.False(first.IsError);

        var replay = await Factory.CreateTokenRequestValidator(authorizationCodeStore: codeStore, refreshTokenStore: refreshTokenStore, referenceTokenStore: referenceTokenStore)
            .ValidateRequestAsync(CodeParameters(handle), client.ToValidationResult());

        Assert.True(replay.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, replay.Error);
        Assert.Null((await refreshTokenStore.GetRefreshTokenAsync(issuedRefreshToken)));
        Assert.Null((await referenceTokenStore.GetReferenceTokenAsync(issuedReferenceToken)));
        Assert.Null((await codeStore.GetAuthorizationCodeAsync(handle)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Redeemed_code_is_kept_as_consumed()
    {
        var client = await _clients.FindEnabledClientByIdAsync("codeclient");
        var codeStore = Factory.CreateAuthorizationCodeStore();
        var handle = await codeStore.StoreAuthorizationCodeAsync(CreateCode(client));

        var result = await Factory.CreateTokenRequestValidator(authorizationCodeStore: codeStore)
            .ValidateRequestAsync(CodeParameters(handle), client.ToValidationResult());

        Assert.False(result.IsError);
        Assert.NotNull((await codeStore.GetAuthorizationCodeAsync(handle)).ConsumedTime);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Code_presented_by_another_client_should_not_be_consumed()
    {
        var owner = await _clients.FindEnabledClientByIdAsync("codeclient");
        var other = await _clients.FindEnabledClientByIdAsync("codeclient.plain");
        var codeStore = Factory.CreateAuthorizationCodeStore();
        var handle = await codeStore.StoreAuthorizationCodeAsync(CreateCode(owner));

        var result = await Factory.CreateTokenRequestValidator(authorizationCodeStore: codeStore)
            .ValidateRequestAsync(CodeParameters(handle), other.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Null((await codeStore.GetAuthorizationCodeAsync(handle)).ConsumedTime);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Refresh_with_narrower_scope_should_succeed()
    {
        var (client, validator, handle) = await CreateRefreshTokenAsync("resource", "resource2", "offline_access");

        var result = await validator.ValidateRequestAsync(RefreshParameters(handle, "resource"), client.ToValidationResult());

        Assert.False(result.IsError);
        Assert.Equivalent(new[] { "resource" }, result.ValidatedRequest.RequestedScopes, strict: true);
        Assert.Equivalent(new[] { "resource" }, result.ValidatedRequest.ValidatedResources.RawScopeValues, strict: true);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Refresh_with_scope_not_originally_granted_should_fail()
    {
        var (client, validator, handle) = await CreateRefreshTokenAsync("resource", "offline_access");

        var result = await validator.ValidateRequestAsync(RefreshParameters(handle, "resource resource2"), client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Refresh_should_fail_when_client_lost_access_to_a_granted_scope()
    {
        var (client, validator, handle) = await CreateRefreshTokenAsync("resource", "resource2", "offline_access");
        client.AllowedScopes.Remove("resource2");

        var result = await validator.ValidateRequestAsync(RefreshParameters(handle), client.ToValidationResult());

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.TokenErrors.InvalidScope, result.Error);
    }

    private static AuthorizationCode CreateCode(Client client) => new()
    {
        CreationTime = DateTime.UtcNow,
        Subject = new IdentityServerUser("123").CreatePrincipal(),
        ClientId = client.ClientId,
        Lifetime = client.AuthorizationCodeLifetime,
        RedirectUri = "https://server/cb",
        RequestedScopes = new List<string> { "openid" }
    };

    private static NameValueCollection CodeParameters(string handle) => new()
    {
        { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.AuthorizationCode },
        { OidcConstants.TokenRequest.Code, handle },
        { OidcConstants.TokenRequest.RedirectUri, "https://server/cb" }
    };

    private static NameValueCollection RefreshParameters(string handle, string scope = null)
    {
        var parameters = new NameValueCollection
        {
            { OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken },
            { OidcConstants.TokenRequest.RefreshToken, handle }
        };

        if (scope != null)
        {
            parameters.Add(OidcConstants.TokenRequest.Scope, scope);
        }

        return parameters;
    }

    private async Task<(Client Client, TokenRequestValidator Validator, string Handle)> CreateRefreshTokenAsync(params string[] scopes)
    {
        var client = await _clients.FindEnabledClientByIdAsync("roclient");
        var store = Factory.CreateRefreshTokenStore();

        var claims = new List<Claim> { new(JwtClaimTypes.Subject, "foo") };
        claims.AddRange(scopes.Select(scope => new Claim(JwtClaimTypes.Scope, scope)));

        var handle = await store.StoreRefreshTokenAsync(new RefreshToken
        {
            AccessToken = new Token("access_token") { Claims = claims, ClientId = client.ClientId },
            Lifetime = 600,
            CreationTime = DateTime.UtcNow
        });

        return (client, Factory.CreateTokenRequestValidator(refreshTokenStore: store), handle);
    }
}
