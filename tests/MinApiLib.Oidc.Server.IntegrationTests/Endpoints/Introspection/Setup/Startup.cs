// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.Introspection.Setup;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = services.AddOidcServer(options =>
        {
            options.IssuerUri = "https://idsvr4";
            options.Endpoints.EnableAuthorizeEndpoint = false;
        });

        builder.AddInMemoryClients(Clients.Get());
        builder.AddInMemoryApiResources(Scopes.GetApis());
        builder.AddInMemoryApiScopes(Scopes.GetScopes());
        builder.AddTestUsers(Users.Get());
        builder.AddDeveloperSigningCredential(persistKey: false);
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory _)
    {
        app.UseRouting();
        app.UseOidcServer();
        app.UseEndpoints(endpoints => endpoints.MapOidcServer());
    }
}
