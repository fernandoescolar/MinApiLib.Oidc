namespace MinApiLib.OidcServer.Hosting;

public class BaseUrlMiddleware
{
    private readonly RequestDelegate _next;

    public BaseUrlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var request = context.Request;

        context.SetOidcServerBasePath(request.PathBase.Value.RemoveTrailingSlash());

        return _next(context);
    }
}
