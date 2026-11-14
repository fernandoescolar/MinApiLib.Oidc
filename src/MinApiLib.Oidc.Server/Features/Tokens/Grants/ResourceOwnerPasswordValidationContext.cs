// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens.Grants;

/// <summary>
/// Context for the validation of a resource owner password credentials request.
/// </summary>
public class ResourceOwnerPasswordValidationContext
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public string UserName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = default!;

    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    public ValidatedTokenRequest Request { get; set; } = default!;

    /// <summary>
    /// Gets or sets the result. Defaults to <c>invalid_grant</c>.
    /// </summary>
    public GrantValidationResult Result { get; set; } = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
}
