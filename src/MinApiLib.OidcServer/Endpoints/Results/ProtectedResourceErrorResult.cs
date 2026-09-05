

namespace MinApiLib.OidcServer.Endpoints.Results;

internal class ProtectedResourceErrorResult : IEndpointResult
{
    public string Error;
    public string? ErrorDescription;

    public ProtectedResourceErrorResult(string error, string? errorDescription = null)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.SetNoCache();

        if (Constants.ProtectedResourceErrorStatusCodes.TryGetValue(Error, out var statusCode))
        {
            context.Response.StatusCode = statusCode;
        }

        if (Error == OidcConstants.ProtectedResourceErrors.ExpiredToken)
        {
            Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
            ErrorDescription = "The access token expired";
        }

        var errorString = $"error=\"{Error}\"";
        if (ErrorDescription.IsMissing())
        {
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"OidcServer\"", errorString }).ToString());
        }
        else
        {
            var errorDescriptionString = $"error_description=\"{ErrorDescription}\"";
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer realm=\"OidcServer\"", errorString, errorDescriptionString }).ToString());
        }

        return Task.CompletedTask;
    }
}
