// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens;

/// <summary>
/// Interface for the token request validator
/// </summary>
public interface ITokenRequestValidator
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <returns></returns>
    Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult);

    /// <summary>
    /// Validates the request together with a validated DPoP proof (RFC 9449). The default implementation ignores the proof.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <param name="clientValidationResult">The client validation result.</param>
    /// <param name="dpopProof">The validated DPoP proof, or <c>null</c> when the request carried none.</param>
    Task<TokenRequestValidationResult> ValidateRequestAsync(NameValueCollection parameters, ClientSecretValidationResult clientValidationResult, DPoPProofValidationResult? dpopProof)
        => ValidateRequestAsync(parameters, clientValidationResult);
}
