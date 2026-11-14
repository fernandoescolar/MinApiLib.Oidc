// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Revocation;

/// <summary>
/// Interface for the userinfo response generator
/// </summary>
public interface ITokenRevocationResponseGenerator
{
    /// <summary>
    /// Creates the revocation endpoint response and processes the revocation request.
    /// </summary>
    /// <param name="validationResult">The userinfo request validation result.</param>
    /// <returns></returns>
    Task<TokenRevocationResponse> ProcessAsync(TokenRevocationRequestValidationResult validationResult);
}
