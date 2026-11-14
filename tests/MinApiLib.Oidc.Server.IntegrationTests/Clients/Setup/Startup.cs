// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.IntegrationTests.Clients.Setup;

public class Startup
{

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication();

        var builder = services.AddOidcServer(options =>
        {
            options.IssuerUri = "https://idsvr4";

            options.Events = new EventsOptions
            {
                RaiseErrorEvents = true,
                RaiseFailureEvents = true,
                RaiseInformationEvents = true,
                RaiseSuccessEvents = true
            };
        });

        builder.AddInMemoryClients(Clients.Get());
        builder.AddInMemoryIdentityResources(Scopes.GetIdentityScopes());
        builder.AddInMemoryApiResources(Scopes.GetApiResources());
        builder.AddInMemoryApiScopes(Scopes.GetApiScopes());
        builder.AddTestUsers(Users.Get());

        builder.AddDeveloperSigningCredential(persistKey: false);

        builder.AddExtensionGrantValidator<ExtensionGrantValidator>();
        builder.AddExtensionGrantValidator<ExtensionGrantValidator2>();
        builder.AddExtensionGrantValidator<NoSubjectExtensionGrantValidator>();
        builder.AddExtensionGrantValidator<DynamicParameterExtensionGrantValidator>();

        builder.AddProfileService<CustomProfileService>();

        builder.AddJwtBearerClientAuthentication();
        builder.AddSecretValidator<ConfirmationSecretValidator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseOidcServer();
        app.UseEndpoints(endpoints => endpoints.MapOidcServer());
    }
}
