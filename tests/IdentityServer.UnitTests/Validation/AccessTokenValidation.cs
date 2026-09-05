namespace IdentityServer.UnitTests.Validation;

public class AccessTokenValidation
{
    private const string Category = "Access token validation";
    private StubClock _clock = new StubClock();

    static AccessTokenValidation()
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    }

    private DateTime now;
    public DateTime UtcNow
    {
        get
        {
            if (now > DateTime.MinValue) return now;
            return DateTime.UtcNow;
        }
    }

    public AccessTokenValidation()
    {
        _clock.UtcNowFunc = () => UtcNow;
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Reference_Token()
    {
        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");

        var handle = await store.StoreReferenceTokenAsync(token);

        var result = await validator.ValidateAccessTokenAsync(handle);

        Assert.False(result.IsError);
        Assert.Equal(8, result.Claims.Count());
        Assert.Equal("roclient", result.Claims.First(c => c.Type == JwtClaimTypes.ClientId).Value);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Reference_Token_with_required_Scope()
    {
        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");

        var handle = await store.StoreReferenceTokenAsync(token);

        var result = await validator.ValidateAccessTokenAsync(handle, "read");

        Assert.False(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Reference_Token_with_missing_Scope()
    {
        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");

        var handle = await store.StoreReferenceTokenAsync(token);

        var result = await validator.ValidateAccessTokenAsync(handle, "missing");

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InsufficientScope, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unknown_Reference_Token()
    {
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateAccessTokenAsync("unknown");

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Reference_Token_Too_Long()
    {
        var validator = Factory.CreateTokenValidator();
        var options = new IdentityServerOptions();

        var longToken = "x".Repeat(options.InputLengthRestrictions.TokenHandle + 1);
        var result = await validator.ValidateAccessTokenAsync(longToken);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Expired_Reference_Token()
    {
        now = DateTime.UtcNow;

        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store, clock: _clock);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 2, "read", "write");
        token.CreationTime = now;

        var handle = await store.StoreReferenceTokenAsync(token);

        now = now.AddSeconds(3);

        var result = await validator.ValidateAccessTokenAsync(handle);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.ExpiredToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Malformed_JWT_Token()
    {
        var validator = Factory.CreateTokenValidator();

        var result = await validator.ValidateAccessTokenAsync("unk.nown");

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_JWT_Token()
    {
        var signer = Factory.CreateDefaultTokenCreator();
        var jwt = await signer.CreateTokenAsync(TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write"));

        var validator = Factory.CreateTokenValidator(null);
        var result = await validator.ValidateAccessTokenAsync(jwt);

        Assert.False(result.IsError);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", Category)]
    public async Task JWT_Token_with_scopes_have_expected_claims(bool flag)
    {
        var options = TestIdentityServerOptions.Create();
        options.EmitScopesAsSpaceDelimitedStringInJwt = flag;

        var signer = Factory.CreateDefaultTokenCreator(options);
        var jwt = await signer.CreateTokenAsync(TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write"));

        var validator = Factory.CreateTokenValidator(null);
        var result = await validator.ValidateAccessTokenAsync(jwt);

        Assert.False(result.IsError);
        Assert.False(string.IsNullOrEmpty(result.Jwt));
        Assert.Equal("roclient", result.Client.ClientId);

        Assert.Equal(8, result.Claims.Count());
        var scopes = result.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToArray();
        Assert.Equal(2, scopes.Count());
        Assert.Equal("read", scopes[0]);
        Assert.Equal("write", scopes[1]);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task JWT_Token_invalid_Issuer()
    {
        var signer = Factory.CreateDefaultTokenCreator();
        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");
        token.Issuer = "invalid";
        var jwt = await signer.CreateTokenAsync(token);

        var validator = Factory.CreateTokenValidator(null);
        var result = await validator.ValidateAccessTokenAsync(jwt);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task JWT_Token_Too_Long()
    {
        var signer = Factory.CreateDefaultTokenCreator();
        var jwt = await signer.CreateTokenAsync(TokenFactory.CreateAccessTokenLong(new Client { ClientId = "roclient" }, "valid", 600, 1000, "read", "write"));

        var validator = Factory.CreateTokenValidator(null);
        var result = await validator.ValidateAccessTokenAsync(jwt);

        Assert.True(result.IsError);
        Assert.Equal(OidcConstants.ProtectedResourceErrors.InvalidToken, result.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_AccessToken_but_Client_not_active()
    {
        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "unknown" }, "valid", 600, "read", "write");

        var handle = await store.StoreReferenceTokenAsync(token);

        var result = await validator.ValidateAccessTokenAsync(handle);

        Assert.True(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task JWT_Token_Multiple_valid_issuers()
    {
        var signer = Factory.CreateDefaultTokenCreator();

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");
        token.Issuer = "https://idsvr.com";
        var jwt1 = await signer.CreateTokenAsync(token);

        token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");
        token.Issuer = "https://another-idsvr.com";
        var jwt2 = await signer.CreateTokenAsync(token);

        var identityServerOptions = new IdentityServerOptions();
        identityServerOptions.TokenValidation.ValidIssuers = new[] { "https://idsvr.com", "https://another-idsvr.com" };
        var validator = Factory.CreateTokenValidator(null, options: identityServerOptions);
        var result1 = await validator.ValidateAccessTokenAsync(jwt1);
        var result2 = await validator.ValidateAccessTokenAsync(jwt2);

        Assert.False(result1.IsError);
        Assert.False(result2.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Reference_Token_keeps_confirmation_claim()
    {
        var store = Factory.CreateReferenceTokenStore();
        var validator = Factory.CreateTokenValidator(store);

        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");
        token.Confirmation = "{\"x5t#S256\":\"thumbprint\"}";

        var handle = await store.StoreReferenceTokenAsync(token);

        var result = await validator.ValidateAccessTokenAsync(handle);

        Assert.False(result.IsError);
        var cnf = result.Claims.Single(c => c.Type == JwtClaimTypes.Confirmation);
        Assert.Equal(token.Confirmation, cnf.Value);
        Assert.Equal(IdentityServerConstants.ClaimValueTypes.Json, cnf.ValueType);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Logout_Token_is_not_accepted_as_access_token()
    {
        var signer = Factory.CreateDefaultTokenCreator();
        var token = TokenFactory.CreateAccessToken(new Client { ClientId = "roclient" }, "valid", 600, "read", "write");
        token.Type = IdentityServerConstants.TokenTypes.LogoutToken;

        var jwt = await signer.CreateTokenAsync(token);
        Assert.Equal("logout+jwt", new JwtSecurityTokenHandler().ReadJwtToken(jwt).Header.Typ);

        var validator = Factory.CreateTokenValidator(null);
        var result = await validator.ValidateAccessTokenAsync(jwt);

        Assert.True(result.IsError);
        Assert.Equal("invalid JWT token type", result.Error);
    }
}
