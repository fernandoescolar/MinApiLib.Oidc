// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

public class ExtensionGrantValidator2 : IExtensionGrantValidator
{
    public Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        var credential = context.Request.Raw.Get("custom_credential");

        if (credential != null)
        {
            context.Result = new GrantValidationResult("818727", "custom");
        }
        else
        {
            context.Result = new GrantValidationResult(MinApiLib.Oidc.Server.Features.Tokens.TokenRequestErrors.InvalidGrant, "invalid custom credential");
        }

        return Task.CompletedTask;
    }

    public string GrantType => "custom2";
}
