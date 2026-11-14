// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Validator for handling API client authentication.
/// </summary>
public interface IApiSecretValidator
{
    /// <summary>
    /// Tries to authenticate an API client based on the incoming request
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    Task<ApiSecretValidationResult> ValidateAsync(HttpContext context);
}
