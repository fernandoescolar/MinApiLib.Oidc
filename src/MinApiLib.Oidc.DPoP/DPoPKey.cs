namespace MinApiLib.Oidc.DPoP;

/// <summary>
/// A key the client proves possession of with DPoP (RFC 9449): an EC P-256 key (<c>ES256</c>) or an RSA key
/// (<c>PS256</c>). Only its public part leaves the application, in the <c>jwk</c> header of each proof.
/// </summary>
public sealed class DPoPKey : IDisposable
{
    // Signature providers are not cached: they would keep a reference to the key after it is disposed, under an id
    // (the thumbprint) that a new instance of the same key shares.
    private static readonly CryptoProviderFactory UncachedProviders = new() { CacheSignatureProviders = false };

    private readonly AsymmetricAlgorithm _key;
    private readonly Dictionary<string, string> _privateJwk;

    private DPoPKey(AsymmetricAlgorithm key, SecurityKey securityKey, string algorithm, Dictionary<string, string> publicJwk, Dictionary<string, string> privateJwk)
    {
        _key = key;
        _privateJwk = privateJwk;
        securityKey.CryptoProviderFactory = UncachedProviders;
        SecurityKey = securityKey;
        Algorithm = algorithm;
        PublicJwk = publicJwk;
        Thumbprint = ComputeThumbprint(publicJwk);
    }

    /// <summary>Gets the signing algorithm of the proofs: <c>ES256</c> or <c>PS256</c>.</summary>
    public string Algorithm { get; }

    /// <summary>Gets the key, to sign with.</summary>
    public SecurityKey SecurityKey { get; }

    /// <summary>Gets the public key as a JSON Web Key: the <c>jwk</c> header of the proofs.</summary>
    public IReadOnlyDictionary<string, string> PublicJwk { get; }

    /// <summary>
    /// Gets the JWK thumbprint (RFC 7638) of the public key: the value of <c>dpop_jkt</c> and of the <c>cnf.jkt</c>
    /// claim of the tokens bound to this key.
    /// </summary>
    public string Thumbprint { get; }

    /// <summary>Creates an EC P-256 key, for <c>ES256</c> proofs.</summary>
    public static DPoPKey CreateECDsa() => FromECDsa(ECDsa.Create(ECCurve.NamedCurves.nistP256));

    /// <summary>Creates an RSA key, for <c>PS256</c> proofs.</summary>
    /// <param name="keySize">The key size in bits. Defaults to 2048.</param>
    public static DPoPKey CreateRsa(int keySize = 2048) => FromRsa(RSA.Create(keySize));

    /// <summary>
    /// Reads a key from a JSON Web Key with its private part, as written by <see cref="ToJwk"/>: an EC P-256 key or an
    /// RSA key.
    /// </summary>
    /// <exception cref="ArgumentException">The JSON is not an EC P-256 or RSA private key.</exception>
    public static DPoPKey FromJwk(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        Dictionary<string, string>? jwk;
        try
        {
            jwk = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("The DPoP key is not a JSON Web Key.", nameof(json), ex);
        }

        string Member(string name) => jwk!.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new ArgumentException($"The DPoP key has no '{name}' member.", nameof(json));

        byte[] Bytes(string name) => Base64UrlEncoder.DecodeBytes(Member(name));

        switch (jwk?.GetValueOrDefault("kty"))
        {
            case "EC" when jwk.GetValueOrDefault("crv") == "P-256":
                return FromECDsa(ECDsa.Create(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    D = Bytes("d"),
                    Q = new ECPoint { X = Bytes("x"), Y = Bytes("y") }
                }));
            case "RSA":
                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Bytes("n"),
                    Exponent = Bytes("e"),
                    D = Bytes("d"),
                    P = Bytes("p"),
                    Q = Bytes("q"),
                    DP = Bytes("dp"),
                    DQ = Bytes("dq"),
                    InverseQ = Bytes("qi")
                });
                return FromRsa(rsa);
            default:
                throw new ArgumentException("The DPoP key must be an EC P-256 or an RSA JSON Web Key.", nameof(json));
        }
    }

    /// <summary>
    /// Writes the key, with its private part, as a JSON Web Key. Keep it protected: whoever has it can use the tokens
    /// bound to it.
    /// </summary>
    public string ToJwk() => JsonSerializer.Serialize(_privateJwk);

    /// <inheritdoc />
    public void Dispose() => _key.Dispose();

    private static DPoPKey FromECDsa(ECDsa key)
    {
        var parameters = key.ExportParameters(includePrivateParameters: true);
        var publicJwk = new Dictionary<string, string>
        {
            ["kty"] = "EC",
            ["crv"] = "P-256",
            ["x"] = Base64UrlEncoder.Encode(parameters.Q.X),
            ["y"] = Base64UrlEncoder.Encode(parameters.Q.Y)
        };
        var privateJwk = new Dictionary<string, string>(publicJwk) { ["d"] = Base64UrlEncoder.Encode(parameters.D) };

        return new DPoPKey(key, new ECDsaSecurityKey(key), SecurityAlgorithms.EcdsaSha256, publicJwk, privateJwk);
    }

    private static DPoPKey FromRsa(RSA key)
    {
        var parameters = key.ExportParameters(includePrivateParameters: true);
        var publicJwk = new Dictionary<string, string>
        {
            ["kty"] = "RSA",
            ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
            ["e"] = Base64UrlEncoder.Encode(parameters.Exponent)
        };
        var privateJwk = new Dictionary<string, string>(publicJwk)
        {
            ["d"] = Base64UrlEncoder.Encode(parameters.D),
            ["p"] = Base64UrlEncoder.Encode(parameters.P),
            ["q"] = Base64UrlEncoder.Encode(parameters.Q),
            ["dp"] = Base64UrlEncoder.Encode(parameters.DP),
            ["dq"] = Base64UrlEncoder.Encode(parameters.DQ),
            ["qi"] = Base64UrlEncoder.Encode(parameters.InverseQ)
        };

        return new DPoPKey(key, new RsaSecurityKey(key), SecurityAlgorithms.RsaSsaPssSha256, publicJwk, privateJwk);
    }

    /// <summary>
    /// RFC 7638: the SHA-256 of the required members of the public key, in lexicographic order, without whitespace.
    /// </summary>
    public static string ComputeThumbprint(IReadOnlyDictionary<string, string> publicJwk)
    {
        var members = publicJwk["kty"] switch
        {
            "EC" => new[] { "crv", "kty", "x", "y" },
            "RSA" => ["e", "kty", "n"],
            var kty => throw new ArgumentException($"Unsupported key type '{kty}'.", nameof(publicJwk))
        };

        var canonical = "{" + string.Join(',', members.Select(m => $"\"{m}\":\"{publicJwk[m]}\"")) + "}";
        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
