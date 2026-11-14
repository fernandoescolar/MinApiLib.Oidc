// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.Introspection.Setup;

public static class Users
{
    public static List<TestUser> Get()
    {
        return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "bob",
                    Password = "bob"
                }
            };
    }
}
