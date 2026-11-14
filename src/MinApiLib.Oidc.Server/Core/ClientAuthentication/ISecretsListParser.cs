// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Parser for finding the best secret in an Enumerable List
/// </summary>
public interface ISecretsListParser
{
    /// <summary>
    /// Tries to find the best secret on the context that can be used for authentication
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>
    /// A parsed secret
    /// </returns>
    Task<ParsedSecret?> ParseAsync(HttpContext context);

    /// <summary>
    /// Gets all available authentication methods.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetAvailableAuthenticationMethods();
}
