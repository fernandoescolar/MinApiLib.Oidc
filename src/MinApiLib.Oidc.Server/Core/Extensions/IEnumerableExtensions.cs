// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Extensions;

public static class IEnumerableExtensions
{
    public static bool HasDuplicates<T, TProp>(this IEnumerable<T> list, Func<T, TProp> selector)
    {
        var d = new HashSet<TProp>();
        foreach (var t in list)
        {
            if (!d.Add(selector(t)))
            {
                return true;
            }
        }
        return false;
    }
}
