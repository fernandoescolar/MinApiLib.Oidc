// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Validation result for API validation
/// </summary>
public class ApiSecretValidationResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the resource.
    /// </summary>
    /// <value>
    /// The resource.
    /// </value>
    public ApiResource Resource { get; set; } = default!;
}
