// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// Implements device flow user code generation
/// </summary>
public interface IUserCodeGenerator
{
    /// <summary>
    /// Gets the type of the user code.
    /// </summary>
    /// <value>
    /// The type of the user code.
    /// </value>
    string UserCodeType { get; }

    /// <summary>
    /// Gets the retry limit.
    /// </summary>
    /// <value>
    /// The retry limit for getting a unique value.
    /// </value>
    int RetryLimit { get; }

    /// <summary>
    /// Generates the user code.
    /// </summary>
    /// <returns></returns>
    Task<string> GenerateAsync();
}
