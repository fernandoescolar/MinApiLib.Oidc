// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.



namespace MinApiLib.Oidc.Server.Features.UserInfo;

internal class ProtectedResourceErrorResult : IEndpointResult
{
    public string Error;
    public string? ErrorDescription;

    public ProtectedResourceErrorResult(string error, string? errorDescription = null)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.SetNoCache();

        if (Constants.ProtectedResourceErrorStatusCodes.TryGetValue(Error, out var statusCode))
        {
            context.Response.StatusCode = statusCode;
        }

        if (Error == OidcConstants.ProtectedResourceErrors.ExpiredToken)
        {
            Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
            ErrorDescription = "The access token expired";
        }

        var errorString = $"error=\"{Error}\"";
        if (ErrorDescription.IsMissing())
        {
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"OidcServer\"", errorString }).ToString());
        }
        else
        {
            var errorDescriptionString = $"error_description=\"{ErrorDescription}\"";
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"OidcServer\"", errorString, errorDescriptionString }).ToString());
        }

        return Task.CompletedTask;
    }
}
