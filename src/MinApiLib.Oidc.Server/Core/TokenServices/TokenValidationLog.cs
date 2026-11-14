// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.TokenServices;

internal class TokenValidationLog
{
    public string? ClientId { get; set; }
    public string? ClientName { get; set; }
    public bool ValidateLifetime { get; set; }

    public string? AccessTokenType { get; set; }
    public string? ExpectedScope { get; set; }
    public string? TokenHandle { get; set; }
    public string? JwtId { get; set; }

    public Dictionary<string, object>? Claims { get; set; }

    public override string ToString()
    {
        return LogSerializer.Serialize(this);
    }
}
