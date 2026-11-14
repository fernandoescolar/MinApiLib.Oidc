// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Discovery;

public class JsonWebKey
{
    public string kty { get; set; } = default!;
    public string use { get; set; } = default!;
    public string kid { get; set; } = default!;
    public string? x5t { get; set; }
    public string? e { get; set; }
    public string? n { get; set; }
    public string[]? x5c { get; set; }
    public string alg { get; set; } = default!;

    public string? x { get; set; }
    public string? y { get; set; }
    public string? crv { get; set; }
}
