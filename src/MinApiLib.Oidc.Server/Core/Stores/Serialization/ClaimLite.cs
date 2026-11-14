// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Stores.Serialization;

public class ClaimLite
{
    public string Type { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? ValueType { get; set; }
}
