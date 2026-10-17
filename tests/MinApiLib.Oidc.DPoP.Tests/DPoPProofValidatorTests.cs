namespace MinApiLib.Oidc.DPoP.Tests;

public class DPoPProofValidatorTests
{
    private static readonly Uri Url = new("https://api.example/orders");

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (DPoPProofValidator Validator, Clock Clock) Create(DPoPProofValidationOptions? options = null)
    {
        var clock = new Clock();
        return (new DPoPProofValidator(options ?? new DPoPProofValidationOptions(), new InMemoryDPoPReplayCache(clock), clock), clock);
    }

    [Fact]
    public async Task Valid_proof_gives_the_key_thumbprint_and_the_nonce()
    {
        var (validator, clock) = Create();
        using var key = DPoPKey.CreateECDsa();
        var proof = DPoPProof.Create(key, HttpMethod.Get, new Uri(Url + "?page=2"), "token", "server-nonce", clock.Now);

        var result = await validator.ValidateAsync(proof, "GET", Url, "token", TestContext.Current.CancellationToken);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(key.Thumbprint, result.Thumbprint);
        Assert.Equal("server-nonce", result.Nonce);
    }

    [Fact]
    public async Task RSA_proofs_are_accepted_too()
    {
        var (validator, clock) = Create();
        using var key = DPoPKey.CreateRsa();

        var result = await validator.ValidateAsync(DPoPProof.Create(key, HttpMethod.Post, Url, issuedAt: clock.Now), "POST", Url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsValid, result.Error);
    }

    [Theory]
    [InlineData("method", "htm does not match")]
    [InlineData("url", "htu does not match")]
    [InlineData("token", "ath does not match")]
    [InlineData("old", "outside the accepted window")]
    [InlineData("future", "outside the accepted window")]
    [InlineData("malformed", "Malformed")]
    public async Task Proof_for_another_request_or_time_is_rejected(string fault, string error)
    {
        var (validator, clock) = Create();
        using var key = DPoPKey.CreateECDsa();
        var issuedAt = fault switch { "old" => clock.Now.AddMinutes(-3), "future" => clock.Now.AddMinutes(3), _ => clock.Now };
        var proof = fault == "malformed" ? "not-a-jwt" : DPoPProof.Create(key, HttpMethod.Get, fault == "url" ? new Uri("https://api.example/invoices") : Url, "token", issuedAt: issuedAt);

        var result = await validator.ValidateAsync(proof, fault == "method" ? "POST" : "GET", Url, fault == "token" ? "other-token" : "token", TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(error, result.Error);
    }

    [Fact]
    public async Task Algorithm_that_is_not_accepted_is_rejected()
    {
        var (validator, clock) = Create(new DPoPProofValidationOptions { SupportedAlgorithms = [SecurityAlgorithms.RsaSsaPssSha256] });
        using var key = DPoPKey.CreateECDsa();

        var result = await validator.ValidateAsync(DPoPProof.Create(key, HttpMethod.Get, Url, issuedAt: clock.Now), "GET", Url, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("unsupported algorithm", result.Error);
    }

    [Fact]
    public async Task Proof_is_accepted_once()
    {
        var (validator, clock) = Create();
        using var key = DPoPKey.CreateECDsa();
        var proof = DPoPProof.Create(key, HttpMethod.Get, Url, issuedAt: clock.Now);

        Assert.True((await validator.ValidateAsync(proof, "GET", Url, cancellationToken: TestContext.Current.CancellationToken)).IsValid);
        Assert.Contains("already been used", (await validator.ValidateAsync(proof, "GET", Url, cancellationToken: TestContext.Current.CancellationToken)).Error);
    }
}
