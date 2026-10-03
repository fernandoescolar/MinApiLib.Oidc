// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityModel 7.0.0 (https://github.com/IdentityModel/IdentityModel): test-only protocol client.

#nullable enable

namespace MinApiLib.Oidc.Server.IntegrationTests.ProtocolClient;

/// <summary>
/// Models an OAuth 2.0 token revocation response
/// </summary>
/// <seealso cref="IdentityModel.Client.ProtocolResponse" />
public class TokenRevocationResponse : ProtocolResponse
{ }