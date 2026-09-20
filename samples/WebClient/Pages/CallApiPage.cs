namespace Samples.WebClient.Pages;

/// <summary>
/// Calls the API with the user's access token and shows the HTTP request and response.
/// </summary>
public sealed record CallApiPage() : GetHandlerAsync<CallApiPage.Request>("/call-api")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? Mode,
        [FromServices] IHttpClientFactory HttpClients);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.HttpContext.AuthenticateAsync(Schemes.Cookie);
        if (!result.Succeeded)
        {
            return Results.Redirect("/");
        }

        var properties = result.Properties!;
        var accessToken = properties.GetTokenValue("access_token")!;
        var tokenType = properties.GetTokenValue("token_type") ?? "Bearer";
        using var key = properties.Items.TryGetValue(DPoPKey.PropertyName, out var keyJson) && keyJson != null ? DPoPKey.FromJson(keyJson) : null;
        var mode = request.Mode ?? "normal";

        var exchanges = await Api.CallAsync(request.HttpClients.CreateClient(Api.HttpClientName), accessToken, tokenType, key, mode, cancellationToken);

        var body = new StringBuilder();
        body.Append(CultureInfo.InvariantCulture, $"""<p>{Html.Encode(Describe(mode, tokenType))}</p><p class="actions"><a href="/">Back</a></p>""");
        for (var i = 0; i < exchanges.Count; i++)
        {
            var exchange = exchanges[i];
            var title = exchanges.Count > 1 ? $" {i + 1}" : string.Empty;
            body.Append(CultureInfo.InvariantCulture, $"<h2>Request{title}</h2>{Html.Pre(exchange.Request)}");
            if (exchange.Proof != null)
            {
                var (header, payload) = Jwt.Decode(exchange.Proof);
                body.Append(CultureInfo.InvariantCulture, $"<p>The DPoP proof, decoded:</p>{Html.Pre(header)}{Html.Pre(payload)}");
            }

            var css = exchange.Status < 300 ? "ok" : "fail";
            var response = $"HTTP/1.1 {exchange.Status} {(HttpStatusCode)exchange.Status}\n"
                + (exchange.Challenge == null ? string.Empty : $"WWW-Authenticate: {exchange.Challenge}\n")
                + "\n" + exchange.Body;
            body.Append(CultureInfo.InvariantCulture, $"""<h2 class="{css}">Response{title}</h2>{Html.Pre(response)}""");
        }

        return Html.Page("Calling the API", body.ToString());
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
