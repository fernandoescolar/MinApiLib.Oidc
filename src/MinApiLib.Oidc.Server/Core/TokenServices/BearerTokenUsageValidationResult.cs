// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.TokenServices;

/// <summary>
/// Models usage of a bearer token
/// </summary>
public class BearerTokenUsageValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the token was found.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the token was found; otherwise, <c>false</c>.
    /// </value>
    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Token))]
    public bool TokenFound { get; set; }

    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    /// <value>
    /// The token.
    /// </value>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the usage type.
    /// </summary>
    /// <value>
    /// The type of the usage.
    /// </value>
    public BearerTokenUsageType UsageType { get; set; }
}
