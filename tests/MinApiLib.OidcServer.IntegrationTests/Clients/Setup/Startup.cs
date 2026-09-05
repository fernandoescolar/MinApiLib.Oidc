namespace MinApiLib.OidcServer.IntegrationTests.Clients.Setup;

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
