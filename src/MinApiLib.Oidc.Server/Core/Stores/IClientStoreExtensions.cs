// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Stores;

/// <summary>
/// Extension for IClientStore
/// </summary>
public static class IClientStoreExtensions
{
    /// <summary>
    /// Finds the enabled client by identifier.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns></returns>
    public static async Task<Client?> FindEnabledClientByIdAsync(this IClientStore store, string clientId)
    {
        var client = await store.FindClientByIdAsync(clientId);
        if (client != null && client.Enabled) return client;

        return null;
    }
}
