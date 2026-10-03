namespace MinApiLib.Oidc.DPoP;

/// <summary>
/// Names that RFC 9449 (OAuth 2.0 Demonstrating Proof of Possession) defines.
/// </summary>
public static class DPoPConstants
{
    /// <summary>The HTTP header that carries the proof, and the <c>Authorization</c> scheme of DPoP-bound tokens.</summary>
    public const string HeaderName = "DPoP";

    /// <summary>The <c>Authorization</c> scheme and the <c>token_type</c> of DPoP-bound access tokens.</summary>
    public const string TokenType = "DPoP";

    /// <summary>The HTTP header with which servers send the nonce to put in the next proof (section 8).</summary>
    public const string NonceHeaderName = "DPoP-Nonce";

    /// <summary>The <c>typ</c> header of proofs.</summary>
    public const string ProofTokenType = "dpop+jwt";

    /// <summary>The authorization request parameter that binds the code to a key thumbprint (section 10).</summary>
    public const string KeyThumbprintParameter = "dpop_jkt";

    /// <summary>The discovery metadata with the algorithms a server accepts for proofs (section 5.1).</summary>
    public const string SigningAlgorithmsSupported = "dpop_signing_alg_values_supported";

    /// <summary>Errors of the token endpoint and the <c>WWW-Authenticate</c> challenge.</summary>
    public static class Errors
    {
        /// <summary>The proof is missing or invalid.</summary>
        public const string InvalidProof = "invalid_dpop_proof";

        /// <summary>The server wants a nonce in the proof, sent in <see cref="NonceHeaderName"/>.</summary>
        public const string UseNonce = "use_dpop_nonce";
    }

    /// <summary>Claims and headers of proofs and DPoP-bound tokens.</summary>
    public static class Claims
    {
        /// <summary>The unique id of the proof.</summary>
        public const string JwtId = "jti";

        /// <summary>The HTTP method of the request.</summary>
        public const string HttpMethod = "htm";

        /// <summary>The URL of the request, without query or fragment.</summary>
        public const string HttpUrl = "htu";

        /// <summary>When the proof was created.</summary>
        public const string IssuedAt = "iat";

        /// <summary>The base64url SHA-256 of the access token sent with the request.</summary>
        public const string AccessTokenHash = "ath";

        /// <summary>The nonce the server sent.</summary>
        public const string Nonce = "nonce";

        /// <summary>The header with the public key of the proof.</summary>
        public const string JsonWebKey = "jwk";

        /// <summary>The confirmation claim of a bound access token.</summary>
        public const string Confirmation = "cnf";

        /// <summary>The member of <see cref="Confirmation"/> with the key thumbprint.</summary>
        public const string JwkThumbprint = "jkt";
    }
}
