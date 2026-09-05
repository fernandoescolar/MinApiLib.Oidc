[[go back to index]](../index.md#documentation-structure)

# Proof-of-Possession Access Tokens

By default, OAuth access tokens are so called *bearer* tokens. This means they are not bound to a client and anybody who possess the token can use it (compare to cash).

*Proof-of-Possession* (short PoP) tokens are bound to the client that requested the token.
If that token leaks, it cannot be used by anyone else (compare to a credit card - well at least in an ideal world).

See [this](https://leastprivilege.com/2020/01/15/oauth-2-0-the-long-road-to-proof-of-possession-access-tokens/) blog post for more history and motivation.

IdentityServer supports PoP tokens by using the [Mutual TLS mechanism](mtls.md#refmutualtls) and DPoP.

## DPoP

With DPoP ([RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) the client proves possession of a private key it holds
by signing a short JWT, the proof, for every request. No client certificates or TLS configuration are needed.

**Token requests.** When a token request carries a `DPoP` header, IdentityServer validates the proof and binds the
access token to the key: the token carries `cnf: { "jkt": "<thumbprint>" }` and the response says
`"token_type": "DPoP"`. Set `Client.RequireDPoP` to refuse token requests without a proof.

The proof must have `typ: dpop+jwt`, an asymmetric algorithm from `DPoPOptions.SupportedSigningAlgorithms`, a public
`jwk` header and the claims `jti`, `htm`, `htu` and `iat`. Each proof is accepted once. `iat` must fall within
`DPoPOptions.ProofTokenValidityDuration` (one minute) plus `DPoPOptions.ClockSkew` (one minute). Server nonces
(`DPoP-Nonce`) are not supported.

**Authorization codes.** A `dpop_jkt` parameter on the authorize request, or a proof sent to the
[pushed authorization endpoint](par.md), binds the code: only a proof of that key can redeem it.

**Refresh tokens.** Refresh tokens of public clients stay bound to the key they were issued with. Confidential clients
can bind each refreshed access token to the key of the current proof.

**Protected resources.** The userinfo endpoint and the [local API](add_apis.md) handler accept
`Authorization: DPoP <token>` with a proof that carries the token hash (`ath`) and is signed by the bound key. A
DPoP-bound token sent as a bearer token is rejected.

Discovery announces the accepted algorithms in `dpop_signing_alg_values_supported`.

[[go back to index]](../index.md#documentation-structure)
