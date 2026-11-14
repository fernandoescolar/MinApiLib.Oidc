// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for the OidcServer middleware
/// </summary>
public class OidcServerMiddlewareOptions
{
    /// <summary>
    /// Callback to wire up an authentication middleware
    /// </summary>
    public Action<IApplicationBuilder> AuthenticationMiddleware { get; set; } = (app) => app.UseAuthentication();
}
