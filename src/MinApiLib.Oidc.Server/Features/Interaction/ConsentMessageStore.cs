// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Interaction;

[RegisterInIServiceCollection(typeof(IConsentMessageStore))]
internal class ConsentMessageStore : IConsentMessageStore
{
    protected readonly MessageCookie<ConsentResponse> Cookie;

    public ConsentMessageStore(MessageCookie<ConsentResponse> cookie)
    {
        Cookie = cookie;
    }

    public virtual Task DeleteAsync(string id)
    {
        Cookie.Clear(id);
        return Task.CompletedTask;
    }

    public virtual Task<Message<ConsentResponse>?> ReadAsync(string id)
    {
        return Task.FromResult(Cookie.Read(id));
    }

    public virtual Task WriteAsync(string id, Message<ConsentResponse> message)
    {
        Cookie.Write(id, message);
        return Task.CompletedTask;
    }
}
