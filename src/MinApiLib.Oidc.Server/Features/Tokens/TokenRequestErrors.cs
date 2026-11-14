// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens;

/// <summary>
/// Token request errors
/// </summary>
public enum TokenRequestErrors
{
    /// <summary>
    /// invalid_request
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// invalid_client
    /// </summary>
    InvalidClient,

    /// <summary>
    /// invalid_grant
    /// </summary>
    InvalidGrant,

    /// <summary>
    /// unauthorized_client
    /// </summary>
    UnauthorizedClient,

    /// <summary>
    /// unsupported_grant_type
    /// </summary>
    UnsupportedGrantType,

    /// <summary>
    /// invalid_scope
    /// </summary>
    InvalidScope,

    /// <summary>
    /// invalid_target
    /// </summary>
    InvalidTarget
}
