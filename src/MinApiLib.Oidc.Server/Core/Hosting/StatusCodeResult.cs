// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Hosting;

/// <summary>
/// Result for a raw HTTP status code
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Hosting.IEndpointResult" />
public class StatusCodeResult : IEndpointResult
{
    /// <summary>
    /// Gets the status code.
    /// </summary>
    /// <value>
    /// The status code.
    /// </value>
    public int StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeResult"/> class.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    public StatusCodeResult(HttpStatusCode statusCode)
    {
        StatusCode = (int)statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeResult"/> class.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    public StatusCodeResult(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Executes the result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns></returns>
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }
}
