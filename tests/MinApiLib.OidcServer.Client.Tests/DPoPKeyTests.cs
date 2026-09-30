namespace MinApiLib.OidcServer.Client.Tests;

public class DPoPKeyTests
{
    [Fact]
    public void Thumbprint_follows_RFC_7638()
    {
        // The example of RFC 7638 section 3.1.
        var jwk = new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["n"] = "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw",
            ["e"] = "AQAB",
            ["alg"] = "RS256",
            ["kid"] = "2011-04-29"
        };

        Assert.Equal("NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs", DPoPKey.ComputeThumbprint(jwk));
    }

    [Theory]
    [InlineData("EC")]
    [InlineData("RSA")]
    public void Thumbprint_matches_the_one_of_Microsoft_IdentityModel(string kty)
    {
        using var key = kty == "EC" ? DPoPKey.CreateECDsa() : DPoPKey.CreateRsa();

        var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(key.SecurityKey);

        Assert.Equal(Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint()), key.Thumbprint);
    }

    [Theory]
    [InlineData("EC", "ES256")]
    [InlineData("RSA", "PS256")]
    public async Task Key_survives_a_round_trip_through_its_JWK(string kty, string algorithm)
    {
        using var key = kty == "EC" ? DPoPKey.CreateECDsa() : DPoPKey.CreateRsa();

        using var restored = DPoPKey.FromJwk(key.ToJwk());

        Assert.Equal(algorithm, restored.Algorithm);
        Assert.Equal(key.Thumbprint, restored.Thumbprint);
        Assert.Equal(key.PublicJwk, restored.PublicJwk);

        // The restored private key signs proofs that the original public key validates.
        var proof = DPoPProof.Create(restored, HttpMethod.Get, new Uri("https://api/orders"));
        Assert.True((await ValidateAsync(proof, key.SecurityKey)).IsValid);
    }

    [Fact]
    public void Public_JWK_has_no_private_members()
    {
        using var key = DPoPKey.CreateRsa();

        Assert.Equal(["e", "kty", "n"], key.PublicJwk.Keys.Order());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"kty":"oct","k":"AAAA"}""")]
    [InlineData("""{"kty":"EC","crv":"P-384","x":"AA","y":"AA","d":"AA"}""")]
    [InlineData("""{"kty":"EC","crv":"P-256","x":"AA","y":"AA"}""")]
    public void Invalid_JWK_is_rejected(string json)
        => Assert.Throws<ArgumentException>(() => DPoPKey.FromJwk(json));

    [Fact]
    public async Task A_disposed_key_does_not_break_a_new_instance_of_the_same_key()
    {
        string jwk;
        using (var key = DPoPKey.CreateECDsa())
        {
            jwk = key.ToJwk();
            DPoPProof.Create(key, HttpMethod.Get, new Uri("https://api/orders"));
        }

        using var again = DPoPKey.FromJwk(jwk);
        var proof = DPoPProof.Create(again, HttpMethod.Get, new Uri("https://api/orders"));

        Assert.True((await ValidateAsync(proof, again.SecurityKey)).IsValid);
    }

    internal static Task<TokenValidationResult> ValidateAsync(string proof, SecurityKey key)
        => new JsonWebTokenHandler().ValidateTokenAsync(proof, new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = false,
            IssuerSigningKey = key,
            ValidTypes = ["dpop+jwt"]
        });
}
