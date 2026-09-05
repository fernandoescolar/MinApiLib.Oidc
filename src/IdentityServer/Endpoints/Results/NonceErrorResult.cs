namespace IdentityServer.Endpoints.Results;

internal class NonceErrorResult : IEndpointResult
{
    public NonceErrorResult(string error, string? errorDescription)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    public string Error { get; }
    public string? ErrorDescription { get; }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 400;
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            error = Error,
            error_description = ErrorDescription,
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string error { get; set; } = default!;
        public string? error_description { get; set; }
    }
}
