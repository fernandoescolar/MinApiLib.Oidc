namespace IdentityServer.Endpoints.Results;

/// <summary>
/// Successful response of the pushed authorization request endpoint (RFC 9126 section 2.2).
/// </summary>
internal class PushedAuthorizationResult : IEndpointResult
{
    public PushedAuthorizationResult(string requestUri, int expiresIn)
    {
        RequestUri = requestUri;
        ExpiresIn = expiresIn;
    }

    public string RequestUri { get; }

    public int ExpiresIn { get; }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();
        context.Response.StatusCode = (int)HttpStatusCode.Created;
        return context.Response.WriteJsonAsync(new ResultDto { request_uri = RequestUri, expires_in = ExpiresIn });
    }

    internal class ResultDto
    {
        public string request_uri { get; set; } = default!;
        public int expires_in { get; set; }
    }
}

/// <summary>
/// Error response of the pushed authorization request endpoint (RFC 9126 section 2.3).
/// </summary>
internal class PushedAuthorizationErrorResult : IEndpointResult
{
    public PushedAuthorizationErrorResult(HttpStatusCode statusCode, string error, string? errorDescription)
    {
        StatusCode = statusCode;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public HttpStatusCode StatusCode { get; }

    public string Error { get; }

    public string? ErrorDescription { get; }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();
        context.Response.StatusCode = (int)StatusCode;
        return context.Response.WriteJsonAsync(new ErrorDto { error = Error, error_description = ErrorDescription });
    }

    internal class ErrorDto
    {
        public string error { get; set; } = default!;
        public string? error_description { get; set; }
    }
}
