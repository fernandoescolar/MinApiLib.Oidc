// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.DeviceFlow;

public class DeviceAuthorizationResponse
{
    public string DeviceCode { get; set; } = default!;
    public string UserCode { get; set; } = default!;
    public string VerificationUri { get; set; } = default!;

    public string? VerificationUriComplete { get; set; }
    public int DeviceCodeLifetime { get; set; }
    public int Interval { get; set; }
}
