// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Stores;

/// <summary>
/// Default authorization code store.
/// </summary>
[RegisterInIServiceCollection(typeof(IAuthorizationCodeStore))]
public class DefaultAuthorizationCodeStore : DefaultGrantStore<AuthorizationCode>, IAuthorizationCodeStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuthorizationCodeStore"/> class.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="handleGenerationService">The handle generation service.</param>
    /// <param name="logger">The logger.</param>
    public DefaultAuthorizationCodeStore(
        IPersistedGrantStore store,
        IPersistentGrantSerializer serializer,
        IHandleGenerationService handleGenerationService,
        ILogger<DefaultAuthorizationCodeStore> logger)
        : base(OidcServerConstants.PersistedGrantTypes.AuthorizationCode, store, serializer, handleGenerationService, logger)
    {
    }

    /// <summary>
    /// Stores the authorization code asynchronous.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    public Task<string> StoreAuthorizationCodeAsync(AuthorizationCode code)
    {
        return CreateItemAsync(code, code.ClientId, code.Subject.GetSubjectId(), code.SessionId, code.Description, code.CreationTime, code.Lifetime);
    }

    /// <summary>
    /// Gets the authorization code asynchronous.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    public Task<AuthorizationCode?> GetAuthorizationCodeAsync(string code)
    {
        return GetItemAsync(code);
    }

    /// <summary>
    /// Removes the authorization code asynchronous.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns></returns>
    public Task RemoveAuthorizationCodeAsync(string code)
    {
        return RemoveItemAsync(code);
    }

    /// <summary>
    /// Stores the code as consumed, keeping it until its original expiration so a replay can be detected.
    /// </summary>
    /// <param name="code">The code handle.</param>
    /// <param name="authorizationCode">The code, with <see cref="AuthorizationCode.ConsumedTime"/> set.</param>
    /// <returns></returns>
    public Task ConsumeAuthorizationCodeAsync(string code, AuthorizationCode authorizationCode)
    {
        ArgumentNullException.ThrowIfNull(authorizationCode);

        return StoreItemAsync(code, authorizationCode, authorizationCode.ClientId, authorizationCode.Subject.GetSubjectId(),
            authorizationCode.SessionId, authorizationCode.Description, authorizationCode.CreationTime,
            authorizationCode.CreationTime.AddSeconds(authorizationCode.Lifetime), authorizationCode.ConsumedTime);
    }
}
