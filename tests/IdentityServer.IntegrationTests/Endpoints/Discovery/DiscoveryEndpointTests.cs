using JsonWebKey = Microsoft.IdentityModel.Tokens.JsonWebKey;

namespace IdentityServer.IntegrationTests.Endpoints.Discovery;

public class DiscoveryEndpointTests
{
    private const string Category = "Discovery endpoint";

    [Fact]
    [Trait("Category", Category)]
    public async Task Issuer_uri_should_be_lowercase()
    {
        using var pipeline = new IdentityServerPipeline();
        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("HTTPS://SERVER/ROOT/.WELL-KNOWN/OPENID-CONFIGURATION", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(json);
        var issuer = data["issuer"].ToString();

        Assert.Equal("https://server/root", issuer);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task when_lower_case_issuer_option_disabled_issuer_uri_should_be_preserved()
    {
        using var pipeline = new IdentityServerPipeline();
        pipeline.Initialize("/ROOT");

        pipeline.Options.LowerCaseIssuerUri = false;

        var result = await pipeline.BackChannelClient.GetAsync("HTTPS://SERVER/ROOT/.WELL-KNOWN/OPENID-CONFIGURATION", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(json);
        var issuer = data["issuer"].ToString();

        Assert.Equal("https://server/ROOT", issuer);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Algorithms_supported_should_match_signing_key()
    {
        var key = CryptoHelper.CreateECDsaSecurityKey(JsonWebKeyECTypes.P256);
        var expectedAlgorithm = SecurityAlgorithms.EcdsaSha256;

        using var pipeline = new IdentityServerPipeline();
        pipeline.OnPostConfigureServices += services =>
        {
            services.AddIdentityServerBuilder()
                .AddSigningCredential(key, expectedAlgorithm);
        };
        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("https://server/root/.well-known/openid-configuration", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(json).AsObject();
        var algorithmsSupported = data["id_token_signing_alg_values_supported"] as JsonArray;

        Assert.Equal(2, algorithmsSupported.Count);
        Assert.NotNull(algorithmsSupported.FirstOrDefault(x => x.ToString().Equals(SecurityAlgorithms.RsaSha256)));
        Assert.NotNull(algorithmsSupported.FirstOrDefault(x => x.ToString().Equals(SecurityAlgorithms.EcdsaSha256)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Jwks_entries_should_countain_crv()
    {
        var ecdsaKey = CryptoHelper.CreateECDsaSecurityKey(JsonWebKeyECTypes.P256);
        var parameters = ecdsaKey.ECDsa.ExportParameters(true);

        using var pipeline = new IdentityServerPipeline();

        var jsonWebKeyFromECDsa = new JsonWebKey()
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Use = "sig",
            Kid = ecdsaKey.KeyId,
            KeyId = ecdsaKey.KeyId,
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y),
            D = Base64UrlEncoder.Encode(parameters.D),
            Crv = JsonWebKeyECTypes.P256,
            Alg = SecurityAlgorithms.EcdsaSha256
        };
        pipeline.OnPostConfigureServices += services =>
        {
            services.AddIdentityServerBuilder()
                .AddSigningCredential(jsonWebKeyFromECDsa, SecurityAlgorithms.EcdsaSha256);
        };

        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("https://server/root/.well-known/openid-configuration/jwks", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(json).AsObject();

        var keys = data["keys"] as JsonArray;
        Assert.NotNull(keys);

        var key = keys[1];
        Assert.NotNull(key);

        var crv = key["crv"];
        Assert.NotNull(crv);

        Assert.Equal(JsonWebKeyECTypes.P256, crv.GetValue<string>());

    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Jwks_entries_should_contain_alg()
    {
        using var pipeline = new IdentityServerPipeline();
        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("https://server/root/.well-known/openid-configuration/jwks", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(json).AsObject();

        var keys = data["keys"] as JsonArray;
        Assert.NotNull(keys);

        var key = keys[0];
        Assert.NotNull(key);

        var alg = key["alg"];
        Assert.NotNull(alg);

        Assert.Equal(Constants.SigningAlgorithms.RSA_SHA_256, alg.GetValue<string>());
    }

    [Theory]
    [InlineData(JsonWebKeyECTypes.P256, SecurityAlgorithms.EcdsaSha256)]
    [InlineData(JsonWebKeyECTypes.P384, SecurityAlgorithms.EcdsaSha384)]
    [InlineData(JsonWebKeyECTypes.P521, SecurityAlgorithms.EcdsaSha512)]
    [Trait("Category", Category)]
    public async Task Jwks_with_ecdsa_should_have_parsable_key(string crv, string alg)
    {
        var key = CryptoHelper.CreateECDsaSecurityKey(crv);

        using var pipeline = new IdentityServerPipeline();
        pipeline.OnPostConfigureServices += services =>
        {
            services.AddIdentityServerBuilder()
                .AddSigningCredential(key, alg);
        };
        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("https://server/root/.well-known/openid-configuration/jwks", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var jwks = new JsonWebKeySet(json);
        var parsedKeys = jwks.GetSigningKeys();

        var matchingKey = parsedKeys.FirstOrDefault(x => x.KeyId == key.KeyId);
        Assert.NotNull(matchingKey);
        Assert.IsType<ECDsaSecurityKey>(matchingKey);
    }

    [Fact]
    public async Task Jwks_with_two_key_using_different_algs_expect_different_alg_values()
    {
        var ecdsaKey = CryptoHelper.CreateECDsaSecurityKey();
        var rsaKey = CryptoHelper.CreateRsaSecurityKey();

        using var pipeline = new IdentityServerPipeline();
        pipeline.OnPostConfigureServices += services =>
        {
            services.AddIdentityServerBuilder()
                .AddSigningCredential(ecdsaKey, "ES256")
                .AddValidationKey(new SecurityKeyInfo { Key = rsaKey, SigningAlgorithm = "RS256" });
        };
        pipeline.Initialize("/ROOT");

        var result = await pipeline.BackChannelClient.GetAsync("https://server/root/.well-known/openid-configuration/jwks", TestContext.Current.CancellationToken);

        var json = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var jwks = new JsonWebKeySet(json);

        Assert.Contains(jwks.Keys, x => x.KeyId == ecdsaKey.KeyId && x.Alg == "ES256");
        Assert.Contains(jwks.Keys, x => x.KeyId == rsaKey.KeyId && x.Alg == "RS256");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unicode_values_in_url_should_be_processed_correctly()
    {
        using var pipeline = new IdentityServerPipeline();
        pipeline.Initialize();

        var result = await pipeline.BackChannelClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = "https://грант.рф",
            Policy =
                {
                    ValidateIssuerName = false,
                    ValidateEndpoints = false,
                    RequireHttps = false,
                    RequireKeySet = false
                }
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("https://грант.рф", result.Issuer);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Discovery_document_should_advertise_iss_parameter_s256_only_and_signing_algorithms()
    {
        using var pipeline = new IdentityServerPipeline();
        pipeline.Initialize();

        var result = await pipeline.BackChannelClient.GetAsync("https://server/.well-known/openid-configuration", TestContext.Current.CancellationToken);
        var data = JsonNode.Parse(await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).AsObject();

        Assert.True(data["authorization_response_iss_parameter_supported"].GetValue<bool>());
        Assert.Equivalent(new[] { "S256" }, data["code_challenge_methods_supported"].AsArray().Select(x => x.ToString()), strict: true);
        Assert.Contains("RS256", data["request_object_signing_alg_values_supported"].AsArray().Select(x => x.ToString()));
        Assert.Equivalent(new[] { "code", "code id_token" }, data["response_types_supported"].AsArray().Select(x => x.ToString()), strict: true);
    }
}
