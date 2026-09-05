namespace MinApiLib.OidcServer.Endpoints.Results;

internal class UserInfoResult : IEndpointResult
{
    public Dictionary<string, object> Claims;

    public UserInfoResult(Dictionary<string, object> claims)
    {
        Claims = claims;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();
        return context.Response.WriteJsonAsync(Claims);
    }
}
