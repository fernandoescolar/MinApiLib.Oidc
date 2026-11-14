// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Models;

public class GrantTypes
{
    public static ICollection<string> Code =>
        new[] { GrantType.AuthorizationCode };

    public static ICollection<string> CodeAndClientCredentials =>
        new[] { GrantType.AuthorizationCode, GrantType.ClientCredentials };

    public static ICollection<string> Hybrid =>
        new[] { GrantType.Hybrid };

    public static ICollection<string> HybridAndClientCredentials =>
        new[] { GrantType.Hybrid, GrantType.ClientCredentials };

    /// <summary>The deprecated implicit grant; requires <see cref="Client.AllowImplicitFlow"/>.</summary>
    public static ICollection<string> Implicit =>
        new[] { GrantType.Implicit };

    /// <summary>The deprecated implicit grant plus client credentials; requires <see cref="Client.AllowImplicitFlow"/>.</summary>
    public static ICollection<string> ImplicitAndClientCredentials =>
        new[] { GrantType.Implicit, GrantType.ClientCredentials };

    /// <summary>The deprecated password grant; requires <see cref="Client.AllowResourceOwnerPasswordFlow"/>.</summary>
    public static ICollection<string> ResourceOwnerPassword =>
        new[] { GrantType.ResourceOwnerPassword };

    /// <summary>The deprecated password grant plus client credentials; requires <see cref="Client.AllowResourceOwnerPasswordFlow"/>.</summary>
    public static ICollection<string> ResourceOwnerPasswordAndClientCredentials =>
        new[] { GrantType.ResourceOwnerPassword, GrantType.ClientCredentials };

    public static ICollection<string> ClientCredentials =>
        new[] { GrantType.ClientCredentials };

    public static ICollection<string> DeviceFlow =>
        new[] { GrantType.DeviceFlow };

    public static ICollection<string> CodeAndNonce =>
        new[] { GrantType.AuthorizationCode, GrantType.NoncePattern };
}
