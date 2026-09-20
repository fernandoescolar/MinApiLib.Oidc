namespace Samples.WebClient.DPoP;

/// <summary>
/// The P-256 key a session proves possession of. Only its public part ever leaves the application, as the
/// <c>jwk</c> header of each proof.
/// </summary>
public sealed class DPoPKey : IDisposable
{
    public const string PropertyName = "sample.dpop_key";

    private readonly ECDsa _key;

    private DPoPKey(ECDsa key)
    {
        _key = key;
        SecurityKey = new ECDsaSecurityKey(key);
        var parameters = key.ExportParameters(includePrivateParameters: false);
        PublicJwk = new Dictionary<string, string>
        {
            ["kty"] = "EC",
            ["crv"] = "P-256",
            ["x"] = Base64UrlEncoder.Encode(parameters.Q.X),
            ["y"] = Base64UrlEncoder.Encode(parameters.Q.Y)
        };
    }

    public ECDsaSecurityKey SecurityKey { get; }

    /// <summary>The public key, as sent in the <c>jwk</c> header.</summary>
    public IReadOnlyDictionary<string, string> PublicJwk { get; }

    /// <summary>The RFC 7638 thumbprint: the value the server puts in <c>cnf.jkt</c>.</summary>
    public string Thumbprint => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(
        $$"""{"crv":"P-256","kty":"EC","x":"{{PublicJwk["x"]}}","y":"{{PublicJwk["y"]}}"}""")));

    public static DPoPKey Create() => new(ECDsa.Create(ECCurve.NamedCurves.nistP256));

    /// <summary>The private key as a JWK, to keep it in the protected session cookie.</summary>
    public string ToJson()
    {
        var parameters = _key.ExportParameters(includePrivateParameters: true);
        return JsonSerializer.Serialize(new Dictionary<string, string>(PublicJwk)
        {
            ["d"] = Base64UrlEncoder.Encode(parameters.D)
        });
    }

    public static DPoPKey FromJson(string json)
    {
        var jwk = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        var key = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64UrlEncoder.DecodeBytes(jwk["d"]),
            Q = new ECPoint { X = Base64UrlEncoder.DecodeBytes(jwk["x"]), Y = Base64UrlEncoder.DecodeBytes(jwk["y"]) }
        });
        return new DPoPKey(key);
    }

    public void Dispose() => _key.Dispose();
}
