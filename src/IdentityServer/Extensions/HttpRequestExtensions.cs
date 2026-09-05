namespace IdentityServer.Extensions;

public static class HttpRequestExtensions
{
    public static string? GetCorsOrigin(this HttpRequest request)
    {
        var origin = request.Headers["Origin"].FirstOrDefault();
        var thisOrigin = request.Scheme + "://" + request.Host;

        if (origin != null && origin != thisOrigin)
        {
            return origin;
        }

        return null;
    }

    internal static bool HasApplicationFormContentType(this HttpRequest request)
    {
        if (request.ContentType is null) return false;

        if (MediaTypeHeaderValue.TryParse(request.ContentType, out var header))
        {
            return header.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
