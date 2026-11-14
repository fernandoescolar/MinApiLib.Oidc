// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.UserInfo;

internal class UserInfoResult : IEndpointResult
{
    public Dictionary<string, object> Claims;

    public UserInfoResult(Dictionary<string, object> claims)
    {
        Claims = claims;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();
        return context.Response.WriteJsonAsync(Claims);
    }
}
