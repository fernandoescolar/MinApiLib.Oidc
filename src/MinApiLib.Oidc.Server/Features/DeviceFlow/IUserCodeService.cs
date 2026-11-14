// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// Implements user code generation
/// </summary>
public interface IUserCodeService
{
    /// <summary>
    /// Gets the user code generator.
    /// </summary>
    /// <param name="userCodeType">Type of user code.</param>
    /// <returns></returns>
    Task<IUserCodeGenerator?> GetGenerator(string userCodeType);
}
