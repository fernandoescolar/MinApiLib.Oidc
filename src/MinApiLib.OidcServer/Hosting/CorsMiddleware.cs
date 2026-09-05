namespace MinApiLib.OidcServer.Hosting;

public static class CorsMiddlewareExtensions
{
    public static void ConfigureCors(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<OidcServerOptions>();
        app.UseCors(options.Cors.CorsPolicyName);
    }
}
