// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Discovery;

/// <summary>
/// Interface for discovery endpoint response generator
/// </summary>
public interface IDiscoveryResponseGenerator
{
    /// <summary>
    /// Creates the discovery document.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="issuerUri">The issuer URI.</param>
    Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri);

    /// <summary>
    /// Creates the JWK document.
    /// </summary>
    Task<IEnumerable<JsonWebKey>> CreateJwkDocumentAsync();
}
