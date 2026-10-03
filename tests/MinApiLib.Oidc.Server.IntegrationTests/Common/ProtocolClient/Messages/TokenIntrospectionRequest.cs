// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel): test-only protocol client.

#nullable enable

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Request for OAuth token introspection
/// </summary>
/// <seealso cref="ProtocolRequest" />
public class TokenIntrospectionRequest : ProtocolRequest
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    /// <value>
    /// The token.
    /// </value>
    public string Token { get; set; } = default!;

    /// <summary>
    /// Gets or sets the token type hint.
    /// </summary>
    /// <value>
    /// The token type hint.
    /// </value>
    public string? TokenTypeHint { get; set; }
}