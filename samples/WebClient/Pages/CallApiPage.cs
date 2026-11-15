namespace Samples.WebClient.Pages;

/// <summary>
/// Calls the API with the user's access token and shows the HTTP request and response.
/// </summary>
public sealed record CallApiPage() : GetHandlerAsync<CallApiPage.Request>("/call-api")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? Mode,
        [FromServices] IHttpClientFactory HttpClients,
        [FromServices] IUserAccessTokenManager Tokens,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.HttpContext.AuthenticateAsync(Schemes.Cookie);
        if (!result.Succeeded)
        {
            return Results.Redirect("/");
        }

        var tokenType = result.Properties!.GetTokenValue("token_type") ?? "Bearer";
        var mode = request.Mode ?? "normal";

        var exchanges = await Api.CallAsync(request.HttpContext, request.HttpClients, request.Tokens, mode, cancellationToken);

        return request.Spa.Render("call-api", "Calling the API", new
        {
            description = Describe(mode, tokenType),
            exchanges = exchanges.Select(exchange => new
            {
                request = exchange.Request,
                proof = exchange.Proof == null ? null : Proof(exchange.Proof),
                status = exchange.Status,
                response = $"HTTP/1.1 {exchange.Status} {(HttpStatusCode)exchange.Status}\n"
                    + (exchange.Challenge == null ? string.Empty : $"WWW-Authenticate: {exchange.Challenge}\n")
                    + "\n" + exchange.Body
            })
        });
    }

    private static object Proof(string proof)
    {
        var (header, payload) = Jwt.Decode(proof);
        return new { header, payload };
    }

    private static string Describe(string mode, string tokenType) => mode switch
    {
        "bearer" => "The DPoP-bound token sent as a plain bearer token, as someone who stole it would: the API rejects it.",
        "other-key" => "The token with a proof signed by another key, as someone who stole the token but not the key would: the API rejects it.",
        "replay" => "The same proof sent twice: the API accepts the first request and rejects the replay.",
        _ when tokenType.Equals("DPoP", StringComparison.OrdinalIgnoreCase) => "The DPoP-bound token with a new proof for this request, signed by the session key.",
        _ => "The bearer token. Whoever has it can use it until it expires."
    };
}
