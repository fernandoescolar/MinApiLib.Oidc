namespace MinApiLib.Oidc.DPoP.Tests;

public class DPoPProofTests
{
    [Fact]
    public async Task Proof_carries_the_request_the_token_hash_and_the_nonce()
    {
        using var key = DPoPKey.CreateECDsa();
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

        var proof = DPoPProof.Create(key, HttpMethod.Post, new Uri("https://api.example/orders?page=2#top"), "the-access-token", "server-nonce", issuedAt);

        var jwt = new JsonWebToken(proof);
        Assert.Equal("dpop+jwt", jwt.Typ);
        Assert.Equal("ES256", jwt.Alg);
        Assert.Equal(key.Thumbprint, DPoPKey.ComputeThumbprint(JsonSerializer.Deserialize<Dictionary<string, string>>(jwt.GetHeaderValue<JsonElement>("jwk").GetRawText())!));
        Assert.Equal("POST", jwt.GetPayloadValue<string>("htm"));
        Assert.Equal("https://api.example/orders", jwt.GetPayloadValue<string>("htu"));
        Assert.Equal(1_800_000_000, jwt.GetPayloadValue<long>("iat"));
        Assert.Equal(Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes("the-access-token"))), jwt.GetPayloadValue<string>("ath"));
        Assert.Equal("server-nonce", jwt.GetPayloadValue<string>("nonce"));
        Assert.True((await DPoPKeyTests.ValidateAsync(proof, key.SecurityKey)).IsValid);
    }

    [Fact]
    public void Proof_for_the_token_endpoint_has_no_ath_or_nonce()
    {
        using var key = DPoPKey.CreateRsa();

        var jwt = new JsonWebToken(DPoPProof.Create(key, HttpMethod.Post, new Uri("https://server/connect/token")));

        Assert.Equal("PS256", jwt.Alg);
        Assert.False(jwt.TryGetPayloadValue<string>("ath", out _));
        Assert.False(jwt.TryGetPayloadValue<string>("nonce", out _));
    }

    [Fact]
    public void Every_proof_has_a_new_jti()
    {
        using var key = DPoPKey.CreateECDsa();
        var url = new Uri("https://api/orders");

        var ids = Enumerable.Range(0, 20).Select(_ => new JsonWebToken(DPoPProof.Create(key, HttpMethod.Get, url)).Id).ToHashSet();

        Assert.Equal(20, ids.Count);
    }
}
