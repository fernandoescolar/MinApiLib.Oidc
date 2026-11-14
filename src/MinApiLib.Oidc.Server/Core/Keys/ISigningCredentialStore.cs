// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Keys;

/// <summary>
/// Interface for a signing credential store
/// </summary>
public interface ISigningCredentialStore
{
    /// <summary>
    /// Gets the signing credentials.
    /// </summary>
    /// <returns></returns>
    Task<SigningCredentials> GetSigningCredentialsAsync();
}
