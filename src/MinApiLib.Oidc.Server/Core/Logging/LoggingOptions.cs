// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Logging;

/// <summary>
/// Options for configuring logging behavior
/// </summary>
public class LoggingOptions
{
    /// <summary>
    ///
    /// </summary>
    public ICollection<string> TokenRequestSensitiveValuesFilter { get; set; } =
        new HashSet<string>
        {
            OidcConstants.TokenRequest.ClientSecret,
            OidcConstants.TokenRequest.Password,
            OidcConstants.TokenRequest.ClientAssertion,
            OidcConstants.TokenRequest.RefreshToken,
            OidcConstants.TokenRequest.DeviceCode,
            OidcConstants.TokenRequest.Code,
            OidcConstants.TokenRequest.CodeVerifier
        };

    /// <summary>
    ///
    /// </summary>
    public ICollection<string> AuthorizeRequestSensitiveValuesFilter { get; set; } =
        new HashSet<string>
        {
            OidcConstants.AuthorizeRequest.IdTokenHint,
            OidcServerConstants.NonceAuthorization.NonceTokenParameter
        };
}
