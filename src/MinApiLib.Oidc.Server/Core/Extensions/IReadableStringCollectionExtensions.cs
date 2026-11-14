// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Extensions;

public static class IReadableStringCollectionExtensions
{
    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            AddField(nv, field.Key, field.Value);
        }

        return nv;
    }

    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IDictionary<string, StringValues> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            AddField(nv, field.Key, field.Value);
        }

        return nv;
    }

    // Parameters may not repeat (RFC 6749 section 3.1), so only the first value is kept, except for parameters that
    // are defined as repeatable: resource (RFC 8707 section 2).
    private static void AddField(NameValueCollection nv, string key, StringValues values)
    {
        if (string.Equals(key, OidcConstants.AuthorizeRequest.Resource, StringComparison.Ordinal))
        {
            foreach (var value in values)
            {
                nv.Add(key, value);
            }

            return;
        }

        nv.Add(key, values.First());
    }
}
