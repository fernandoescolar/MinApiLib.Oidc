// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Cors;

/// <summary>
/// Caching decorator for ICorsPolicyService
/// </summary>
/// <seealso cref="MinApiLib.Oidc.Server.Core.Cors.ICorsPolicyService" />
public class CachingCorsPolicyService<T> : ICorsPolicyService
    where T : ICorsPolicyService
{
    /// <summary>
    /// Class to model entries in CORS origin cache.
    /// </summary>
    public class CorsCacheEntry
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        public CorsCacheEntry(bool allowed)
        {
            Allowed = allowed;
        }

        /// <summary>
        /// Is origin allowed.
        /// </summary>
        public bool Allowed { get; }
    }

    private readonly OidcServerOptions _options;
    private readonly ICache<CorsCacheEntry> _corsCache;
    private readonly ICorsPolicyService _inner;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingCorsPolicyService{T}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="inner">The inner.</param>
    /// <param name="corsCache">The CORS origin cache.</param>
    /// <param name="logger">The logger.</param>
    public CachingCorsPolicyService(OidcServerOptions options,
        T inner,
        ICache<CorsCacheEntry> corsCache,
        ILogger<CachingCorsPolicyService<T>> logger)
    {
        _options = options;
        _inner = inner;
        _corsCache = corsCache;
        _logger = logger;
    }

    /// <summary>
    /// Determines whether origin is allowed.
    /// </summary>
    /// <param name="origin">The origin.</param>
    /// <returns></returns>
    public virtual async Task<bool> IsOriginAllowedAsync(string origin)
    {
        var entry = await _corsCache.GetAsync(origin,
            _options.Caching.CorsExpiration,
            async () => new CorsCacheEntry(await _inner.IsOriginAllowedAsync(origin)),
            _logger);

        // the factory never returns null, so neither does the cache
        return entry!.Allowed;
    }
}
