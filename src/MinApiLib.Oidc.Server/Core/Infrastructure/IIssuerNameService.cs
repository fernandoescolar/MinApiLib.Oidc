// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Infrastructure;

/// <summary>
/// Resolves the issuer identifier that tokens, discovery and validation use.
/// </summary>
public interface IIssuerNameService
{
    /// <summary>
    /// Returns the issuer identifier for the current context.
    /// </summary>
    /// <returns>The issuer identifier, without a trailing slash.</returns>
    Task<string> GetCurrentAsync();
}
