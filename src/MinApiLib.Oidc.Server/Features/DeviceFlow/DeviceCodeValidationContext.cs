// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

/// <summary>
/// Validation result for device code validation request.
/// </summary>
public class DeviceCodeValidationContext
{
    /// <summary>
    /// Gets or sets the device code.
    /// </summary>
    /// <value>
    /// The device code.
    /// </value>
    public string DeviceCode { get; set; } = default!;

    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    /// <value>
    /// The request.
    /// </value>
    public ValidatedTokenRequest Request { get; set; } = default!;

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    /// <value>
    /// The result.
    /// </value>
    public TokenRequestValidationResult Result { get; set; } = default!;
}
