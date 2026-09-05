namespace IdentityServer.Hosting;

public static class CorsMiddlewareExtensions
{
    public static void ConfigureCors(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IdentityServerOptions>();
        app.UseCors(options.Cors.CorsPolicyName);
    }
}
