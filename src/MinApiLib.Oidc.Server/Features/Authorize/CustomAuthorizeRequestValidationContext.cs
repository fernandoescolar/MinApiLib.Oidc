// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Context for custom authorize request validation.
/// </summary>
public class CustomAuthorizeRequestValidationContext
{
    /// <summary>
    /// The result of custom validation.
    /// </summary>
    public AuthorizeRequestValidationResult Result { get; set; } = default!;
}
