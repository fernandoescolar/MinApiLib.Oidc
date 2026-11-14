// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Interface for the authorize response generator
/// </summary>
public interface IAuthorizeResponseGenerator
{
    /// <summary>
    /// Creates the response
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns></returns>
    Task<AuthorizeResponse> CreateResponseAsync(ValidatedAuthorizeRequest request);
}
