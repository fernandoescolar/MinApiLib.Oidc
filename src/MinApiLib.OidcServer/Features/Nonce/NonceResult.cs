namespace MinApiLib.OidcServer.Features.Nonce;

internal class NonceResult : IEndpointResult
{
    public NonceResult(NonceResponse nonceResponse)
    {
        Nonce = nonceResponse.Nonce;
        ExpiresIn = nonceResponse.Lifetime;
    }

    public string Nonce { get; }
    public int ExpiresIn { get; }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.SetNoCache();

        var dto = new ResultDto
        {
            nonce = Nonce,
            expires_in = ExpiresIn,
        };

        return context.Response.WriteJsonAsync(dto);
    }

    internal class ResultDto
    {
        public string nonce { get; set; } = default!;
        public int expires_in { get; set; }
    }
}
