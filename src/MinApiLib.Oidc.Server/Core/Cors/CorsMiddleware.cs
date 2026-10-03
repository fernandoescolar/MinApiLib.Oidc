namespace MinApiLib.Oidc.Server.Core.Cors;

public static class CorsMiddlewareExtensions
{
    public static void ConfigureCors(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<OidcServerOptions>();
        app.UseCors(options.Cors.CorsPolicyName);
    }
}
