// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Stores;

/// <summary>
/// IMessageStore implementation that uses data protection to protect message.
/// </summary>
/// <typeparam name="TModel"></typeparam>
[RegisterInIServiceCollection(typeof(IMessageStore<>))]
public class ProtectedDataMessageStore<TModel> : IMessageStore<TModel>
{
    private const string Purpose = "MinApiLib.Oidc.Server.Stores.ProtectedDataMessageStore";

    /// <summary>
    /// The data protector.
    /// </summary>
    protected readonly IDataProtector Protector;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public ProtectedDataMessageStore(IDataProtectionProvider provider, ILogger<ProtectedDataMessageStore<TModel>> logger)
    {
        Protector = provider.CreateProtector(Purpose);
        Logger = logger;
    }

    /// <inheritdoc />
    public virtual Task<Message<TModel>?> ReadAsync(string? id)
    {
        Message<TModel>? result = null;

        if (!String.IsNullOrWhiteSpace(id))
        {
            try
            {
                var bytes = Base64Url.DecodeFromChars(id);
                bytes = Protector.Unprotect(bytes);
                var json = Encoding.UTF8.GetString(bytes);
                result = ObjectSerializer.FromString<Message<TModel>>(json);
            }
            catch (Exception ex)
            {
                SecretLog.ProtectedMessageReadFailed(Logger, ex);
            }
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public virtual Task<string?> WriteAsync(Message<TModel> message)
    {
        string? value = null;

        try
        {
            var json = ObjectSerializer.ToString(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            bytes = Protector.Protect(bytes);
            value = Base64Url.EncodeToString(bytes);
        }
        catch (Exception ex)
        {
            SecretLog.ProtectedMessageWriteFailed(Logger, ex);
        }

        return Task.FromResult(value);
    }
}
