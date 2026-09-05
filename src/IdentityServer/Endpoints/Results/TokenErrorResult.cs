namespace IdentityServer.Endpoints.Results;

internal class TokenErrorResult : IEndpointResult
{
    public TokenErrorResponse Response { get; }

    public TokenErrorResult(TokenErrorResponse error)
    {
        if (error.Error.IsMissing()) throw new ArgumentNullException(nameof(error), "Error must be set");

        Response = error;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 400;
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            error = Response.Error,
            error_description = Response.ErrorDescription,

            custom = Response.Custom
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string error { get; set; } = default!;
        public string? error_description { get; set; }

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, object>? custom { get; set; }
    }
}
