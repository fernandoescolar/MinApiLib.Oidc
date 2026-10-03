using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

/// <summary>
/// Creates DPoP proofs (RFC 9449) signed with an ephemeral P-256 key.
/// </summary>
internal sealed class DPoPProof : IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsaSecurityKey _securityKey;
    private readonly Microsoft.IdentityModel.Tokens.JsonWebKey _publicJwk;

    public DPoPProof()
    {
        _securityKey = new ECDsaSecurityKey(_key);
        var parameters = _key.ExportParameters(false);
        _publicJwk = new Microsoft.IdentityModel.Tokens.JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Crv = JsonWebKeyECTypes.P256,
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y)
        };
        Thumbprint = Base64UrlEncoder.Encode(_publicJwk.ComputeJwkThumbprint());
    }

    public string Thumbprint { get; }

    public Dictionary<string, object> PublicJwk => new()
    {
        { "kty", _publicJwk.Kty },
        { "crv", _publicJwk.Crv },
        { "x", _publicJwk.X },
        { "y", _publicJwk.Y }
    };

    public string Create(
        string method,
        string url,
        string accessToken = null,
        DateTimeOffset? issuedAt = null,
        string jti = null,
        string type = "dpop+jwt",
        Dictionary<string, object> jwk = null,
        SecurityKey signingKey = null,
        IEnumerable<string> omitClaims = null,
        bool includeJwk = true,
        SigningCredentials signingCredentials = null)
    {
        var claims = new Dictionary<string, object>
        {
            { "jti", jti ?? Guid.NewGuid().ToString("N") },
            { "htm", method },
            { "htu", url },
            { "iat", (issuedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() }
        };
        foreach (var claim in omitClaims ?? [])
        {
            claims.Remove(claim);
        }

        if (accessToken != null)
        {
            claims.Add("ath", Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(accessToken))));
        }

        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Claims = claims,
            TokenType = type,
            AdditionalHeaderClaims = includeJwk ? new Dictionary<string, object> { { "jwk", jwk ?? PublicJwk } } : null,
            SigningCredentials = signingCredentials ?? new SigningCredentials(signingKey ?? _securityKey, SecurityAlgorithms.EcdsaSha256)
        });
    }

    public Dictionary<string, object> PrivateJwk()
    {
        var jwk = PublicJwk;
        jwk.Add("d", Base64UrlEncoder.Encode(_key.ExportParameters(true).D));
        return jwk;
    }

    public SecurityKey SigningKey => _securityKey;

    public void Dispose() => _key.Dispose();
}
