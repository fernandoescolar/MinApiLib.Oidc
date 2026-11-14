// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Models;

/// <summary>
/// Protocol values the model types need for their defaults. They mirror the public constants in
/// MinApiLib.Oidc.Server.OidcServerConstants, which live in the OidcServer assembly; a unit test keeps them in sync.
/// </summary>
internal static class ModelConstants
{
    public const string OidcServerAuthenticationType = "OidcServer";
    public const string OpenIdConnectProtocolType = "oidc";
    public const string SharedSecretType = "SharedSecret";
}
