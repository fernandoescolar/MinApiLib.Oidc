// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

using MinApiLib.Oidc.Server.Core.Configuration;
using MinApiLib.Oidc.Server.Core.Extensions;
using MinApiLib.Oidc.Server.Core.Infrastructure;
using MinApiLib.Oidc.Server.Core.Stores;
using MinApiLib.Oidc.Server.Core.TokenServices;
using MinApiLib.Oidc.Server.Core.Users;
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the OidcServer builder
/// </summary>
public static class OidcServerBuilderExtensions
{
    /// <summary>
    /// Adds test users.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="users">The users.</param>
    /// <returns></returns>
    public static IOidcServerBuilder AddTestUsers(this IOidcServerBuilder builder, List<TestUser> users)
    {
        builder.Services.AddSingleton(new TestUserStore(users));
        builder.AddProfileService<TestUserProfileService>();
        builder.AddResourceOwnerValidator<TestUserResourceOwnerPasswordValidator>();

        return builder;
    }
}
