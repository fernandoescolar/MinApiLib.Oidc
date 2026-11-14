// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.TokenServices;

public enum BearerTokenUsageType
{
    AuthorizationHeader = 0,
    PostBody = 1,
    QueryString = 2,

    /// <summary>
    /// The token was sent in the Authorization header with the DPoP scheme (RFC 9449 section 7.1).
    /// </summary>
    DPoPAuthorizationHeader = 3
}
