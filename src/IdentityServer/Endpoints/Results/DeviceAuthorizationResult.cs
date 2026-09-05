namespace IdentityServer.Endpoints.Results;

internal class DeviceAuthorizationResult : IEndpointResult
{
    public DeviceAuthorizationResponse Response { get; }

    public DeviceAuthorizationResult(DeviceAuthorizationResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            device_code = Response.DeviceCode,
            user_code = Response.UserCode,
            verification_uri = Response.VerificationUri,
            verification_uri_complete = Response.VerificationUriComplete,
            expires_in = Response.DeviceCodeLifetime,
            interval = Response.Interval
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string device_code { get; set; } = default!;
        public string user_code { get; set; } = default!;
        public string verification_uri { get; set; } = default!;
        public string? verification_uri_complete { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }
    }
}
