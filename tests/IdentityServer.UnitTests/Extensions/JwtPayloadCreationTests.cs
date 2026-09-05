namespace IdentityServer.UnitTests.Extensions;

public class JwtPayloadCreationTests
{
    private Token _token;

    public JwtPayloadCreationTests()
    {
        var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Scope, "scope1"),
                new Claim(JwtClaimTypes.Scope, "scope2"),
                new Claim(JwtClaimTypes.Scope, "scope3"),
            };

        _token = new Token(OidcConstants.TokenTypes.AccessToken)
        {
            CreationTime = DateTime.UtcNow,
            Issuer = "issuer",
            Lifetime = 60,
            Claims = claims.Distinct(new ClaimComparer()).ToList(),
            ClientId = "client"
        };
    }

    [Fact]
    public void Should_create_scopes_as_array_by_default()
    {
        var options = new IdentityServerOptions();
        var payload = _token.CreateJwtPayload(TimeProvider.System, options, TestLogger.Create<JwtPayloadCreationTests>());

        Assert.NotNull(payload);
        var scopes = payload.Claims.Where(c => c.Type == JwtClaimTypes.Scope).ToArray();
        Assert.Equal(3, scopes.Count());
        Assert.Equal("scope1", scopes[0].Value);
        Assert.Equal("scope2", scopes[1].Value);
        Assert.Equal("scope3", scopes[2].Value);
    }

    [Fact]
    public void Should_create_scopes_as_string()
    {
        var options = new IdentityServerOptions
        {
            EmitScopesAsSpaceDelimitedStringInJwt = true
        };

        var payload = _token.CreateJwtPayload(TimeProvider.System, options, TestLogger.Create<JwtPayloadCreationTests>());

        Assert.NotNull(payload);
        var scopes = payload.Claims.Where(c => c.Type == JwtClaimTypes.Scope).ToList();
        Assert.Single(scopes);
        Assert.Equal("scope1 scope2 scope3", scopes.First().Value);
    }
}
