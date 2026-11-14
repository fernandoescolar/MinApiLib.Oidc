// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Cors;

/// <summary>
/// Default CORS policy service.
/// </summary>
[RegisterInIServiceCollection(typeof(ICorsPolicyService))]
public class DefaultCorsPolicyService : ICorsPolicyService
{
    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCorsPolicyService"/> class.
    /// </summary>
    public DefaultCorsPolicyService(ILogger<DefaultCorsPolicyService> logger)
    {
        Logger = logger;
        AllowedOrigins = new HashSet<string>();
    }

    /// <summary>
    /// The list allowed origins that are allowed.
    /// </summary>
    /// <value>
    /// The allowed origins.
    /// </value>
    public ICollection<string> AllowedOrigins { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all origins are allowed.
    /// </summary>
    /// <value>
    ///   <c>true</c> if allow all; otherwise, <c>false</c>.
    /// </value>
    public bool AllowAll { get; set; }

    /// <summary>
    /// Determines whether the origin allowed.
    /// </summary>
    /// <param name="origin">The origin.</param>
    /// <returns></returns>
    public virtual Task<bool> IsOriginAllowedAsync(string origin)
    {
        if (!String.IsNullOrWhiteSpace(origin))
        {
            if (AllowAll)
            {
                ServicesLog.CorsAllowAllOriginAllowed(Logger, origin);
                return Task.FromResult(true);
            }

            if (AllowedOrigins != null)
            {
                if (AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    ServicesLog.CorsAllowedOriginsOriginAllowed(Logger, origin);
                    return Task.FromResult(true);
                }
                else
                {
                    ServicesLog.CorsAllowedOriginsOriginNotAllowed(Logger, origin);
                }
            }

            ServicesLog.CorsOriginNotAllowed(Logger, origin);
        }

        return Task.FromResult(false);
    }
}
