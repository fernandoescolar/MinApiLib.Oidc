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
