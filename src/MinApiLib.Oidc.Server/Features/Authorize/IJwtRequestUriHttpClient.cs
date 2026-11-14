// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Models making HTTP requests for JWTs from the authorize endpoint.
/// </summary>
public interface IJwtRequestUriHttpClient
{
    /// <summary>
    /// Fetches a JWT from the specified <paramref name="url"/> on behalf of the given <paramref name="client"/>.
    /// </summary>
    /// <param name="url">The absolute URL of the JWT resource (<c>request_uri</c> parameter).</param>
    /// <param name="client">The OAuth client making the authorization request.</param>
    /// <param name="cancellationToken">A token to cancel the remote HTTP request.</param>
    /// <returns>
    /// The raw JWT string if the request succeeds; <c>null</c> if the URL is invalid,
    /// targets a private network, the response exceeds the configured size limit,
    /// or the server returns a non-success status code.
    /// </returns>
    Task<string?> GetJwtAsync(string url, Client client, CancellationToken cancellationToken = default);
}
