// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Revocation;

/// <summary>
/// Models the validation result of access tokens and id tokens.
/// </summary>
public class TokenRevocationRequestValidationResult : ValidationResult
{
    /// <summary>
    /// Gets or sets the token type hint.
    /// </summary>
    /// <value>
    /// The token type hint.
    /// </value>
    public string? TokenTypeHint { get; set; }

    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    /// <value>
    /// The token.
    /// </value>
    public string Token { get; set; } = default!;

    /// <summary>
    /// Gets or sets the client.
    /// </summary>
    /// <value>
    /// The client.
    /// </value>
    public Client Client { get; set; } = default!;
}
