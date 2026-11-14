// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Hosting;

public class BaseUrlMiddleware
{
    private readonly RequestDelegate _next;

    public BaseUrlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var request = context.Request;

        context.SetOidcServerBasePath(request.PathBase.Value.RemoveTrailingSlash());

        return _next(context);
    }
}
