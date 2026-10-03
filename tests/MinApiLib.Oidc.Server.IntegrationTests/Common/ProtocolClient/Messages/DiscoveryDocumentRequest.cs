// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel): test-only protocol client.

#nullable enable

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Request for OpenID Connect discovery document
/// </summary>
public class DiscoveryDocumentRequest : ProtocolRequest
{
    /// <summary>
    /// Gets or sets the policy.
    /// </summary>
    /// <value>
    /// The policy.
    /// </value>
    public DiscoveryPolicy Policy { get; set; } = new();
}