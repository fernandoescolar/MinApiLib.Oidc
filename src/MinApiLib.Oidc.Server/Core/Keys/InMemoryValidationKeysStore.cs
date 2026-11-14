// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Keys;

/// <summary>
/// The default validation key store
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Keys.IValidationKeysStore" />
public class InMemoryValidationKeysStore : IValidationKeysStore
{
    private readonly IEnumerable<SecurityKeyInfo> _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryValidationKeysStore"/> class.
    /// </summary>
    /// <param name="keys">The keys.</param>
    /// <exception cref="System.ArgumentNullException">keys</exception>
    public InMemoryValidationKeysStore(IEnumerable<SecurityKeyInfo> keys)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    /// <summary>
    /// Gets all validation keys.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
    {
        return Task.FromResult(_keys);
    }
}
