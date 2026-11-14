// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Authorize;

internal class QueryStringAuthorizationParametersMessageStore : IAuthorizationParametersMessageStore
{
    private readonly TimeProvider _clock;

    public QueryStringAuthorizationParametersMessageStore(TimeProvider clock)
    {
        _clock = clock;
    }

    public Task<string> WriteAsync(Message<IDictionary<string, string[]>> message)
    {
        var queryString = message.Data.FromFullDictionary().ToQueryString();
        return Task.FromResult(queryString);
    }

    public Task<Message<IDictionary<string, string[]>>> ReadAsync(string? id)
    {
        var values = id.ReadQueryStringAsNameValueCollection();
        var msg = new Message<IDictionary<string, string[]>>(values.ToFullDictionary(), _clock.GetUtcNow().UtcDateTime);
        return Task.FromResult(msg);
    }

    public Task DeleteAsync(string? id)
    {
        return Task.CompletedTask;
    }
}
