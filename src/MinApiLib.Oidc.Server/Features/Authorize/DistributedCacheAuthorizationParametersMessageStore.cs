// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

/// <summary>
/// Implementation of IAuthorizationParametersMessageStore that uses the IDistributedCache.
/// </summary>
public class DistributedCacheAuthorizationParametersMessageStore : IAuthorizationParametersMessageStore
{
    private readonly IDistributedCache _distributedCache;
    private readonly IHandleGenerationService _handleGenerationService;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="distributedCache"></param>
    /// <param name="handleGenerationService"></param>
    public DistributedCacheAuthorizationParametersMessageStore(IDistributedCache distributedCache, IHandleGenerationService handleGenerationService)
        : this(distributedCache, handleGenerationService, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance using the given clock.
    /// </summary>
    /// <param name="distributedCache">The distributed cache.</param>
    /// <param name="handleGenerationService">The handle generation service.</param>
    /// <param name="clock">The clock.</param>
    public DistributedCacheAuthorizationParametersMessageStore(IDistributedCache distributedCache, IHandleGenerationService handleGenerationService, TimeProvider clock)
    {
        _distributedCache = distributedCache;
        _handleGenerationService = handleGenerationService;
        _clock = clock;
    }

    private string CacheKeyPrefix => "DistributedCacheAuthorizationParametersMessageStore";

    /// <inheritdoc/>
    public async Task<string> WriteAsync(Message<IDictionary<string, string[]>> message)
    {
        message.Data.Remove(OidcConstants.AuthorizeRequest.RequestUri);

        var key = await _handleGenerationService.GenerateAsync();
        var cacheKey = $"{CacheKeyPrefix}-{key}";

        var json = ObjectSerializer.ToString(message);

        var options = new DistributedCacheEntryOptions();
        options.SetSlidingExpiration(Constants.DefaultCacheDuration);

        await _distributedCache.SetStringAsync(cacheKey, json, options);

        return key;
    }

    /// <inheritdoc/>
    public async Task<Message<IDictionary<string, string[]>>> ReadAsync(string? id)
    {
        var cacheKey = $"{CacheKeyPrefix}-{id}";
        var json = await _distributedCache.GetStringAsync(cacheKey);

        if (json == null)
        {
            return new Message<IDictionary<string, string[]>>(new Dictionary<string, string[]>(), _clock.GetUtcNow().UtcDateTime);
        }

        return ObjectSerializer.FromString<Message<IDictionary<string, string[]>>>(json)
            ?? new Message<IDictionary<string, string[]>>(new Dictionary<string, string[]>(), _clock.GetUtcNow().UtcDateTime);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string? id)
    {
        return _distributedCache.RemoveAsync($"{CacheKeyPrefix}-{id}");
    }
}
