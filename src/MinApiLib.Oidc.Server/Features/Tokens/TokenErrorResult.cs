// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Features.Tokens;

internal class TokenErrorResult : IEndpointResult
{
    public TokenErrorResponse Response { get; }

    public TokenErrorResult(TokenErrorResponse error)
    {
        if (error.Error.IsMissing()) throw new ArgumentNullException(nameof(error), "Error must be set");

        Response = error;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        ClientAuthenticationChallenge.SetStatusCode(context, Response.Error);
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            error = Response.Error,
            error_description = Response.ErrorDescription,

            custom = Response.Custom
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string error { get; set; } = default!;
        public string? error_description { get; set; }

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, object>? custom { get; set; }
    }
}
