namespace MinApiLib.Oidc.Server.Features.Tokens;

internal class TokenResult : IEndpointResult
{
    public TokenResponse Response { get; set; }

    public TokenResult(TokenResponse response)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            id_token = Response.IdentityToken,
            access_token = Response.AccessToken,
            refresh_token = Response.RefreshToken,
            expires_in = Response.AccessTokenLifetime,
            token_type = Response.AccessTokenType ?? OidcConstants.TokenResponse.BearerTokenType,
            scope = Response.Scope,

            Custom = Response.Custom
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string? id_token { get; set; }
        public string access_token { get; set; } = default!;
        public int expires_in { get; set; }
        public string token_type { get; set; } = default!;
        public string? refresh_token { get; set; }
        public string? scope { get; set; }

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, object>? Custom { get; set; }
    }
}
