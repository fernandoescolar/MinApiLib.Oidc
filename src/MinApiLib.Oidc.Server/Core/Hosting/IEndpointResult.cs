// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Hosting;

/// <summary>
/// Endpoint result. It is an ASP.NET Core <see cref="IResult"/>, so endpoint handlers' results can be returned
/// from minimal API and endpoint routing handlers as well.
/// </summary>
public interface IEndpointResult : IResult
{
    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    new Task ExecuteAsync(HttpContext context);
}
