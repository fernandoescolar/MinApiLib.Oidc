namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for the OidcServer middleware
/// </summary>
public class OidcServerMiddlewareOptions
{
    /// <summary>
    /// Callback to wire up an authentication middleware
    /// </summary>
    public Action<IApplicationBuilder> AuthenticationMiddleware { get; set; } = (app) => app.UseAuthentication();
}
