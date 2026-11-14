// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.DPoP;

/// <summary>
/// Interface for replay cache implementations
/// </summary>
public interface IReplayCache
{
    /// <summary>
    /// Adds a handle to the cache
    /// </summary>
    /// <param name="purpose"></param>
    /// <param name="handle"></param>
    /// <param name="expiration"></param>
    /// <returns></returns>
    Task AddAsync(string purpose, string handle, DateTimeOffset expiration);

    /// <summary>
    /// Checks if a cached handle exists
    /// </summary>
    /// <param name="purpose"></param>
    /// <param name="handle"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(string purpose, string handle);
}
