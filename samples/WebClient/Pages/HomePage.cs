namespace Samples.WebClient.Pages;

/// <summary>
/// Signed out, the two clients of the server and the passkey sign-in (<c>home</c>). Signed in, how the user signed
/// in, the access token and, with DPoP, the session key (<c>session</c>).
/// </summary>
public sealed record HomePage() : GetHandlerAsync<HomePage.Request>("/")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromServices] IUserAccessTokenManager Tokens,
        [FromServices] IConfiguration Configuration,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var result = await http.AuthenticateAsync(Schemes.Cookie);
        var passkeysUrl = request.Configuration["Authority"] + "/account/passkeys";
        if (!result.Succeeded)
        {
            return request.Spa.Render("home", "Home", new { passkeysUrl });
        }

        // The token the API calls use: the library reads it from the session, refreshed if it is about to expire.
        var token = (await request.Tokens.GetAccessTokenAsync(http, Schemes.Cookie, cancellationToken: cancellationToken))!;
        var (header, payload) = Jwt.Decode(token.Value);
        var principal = result.Principal!;

        return request.Spa.Render("session", $"Signed in as {principal.Identity!.Name}", new
        {
            name = principal.Identity.Name,
            scheme = Schemes.Of(result.Properties),
            amr = principal.FindAll("amr").Select(c => c.Value),
            acr = principal.FindFirst("acr")?.Value,
            tokenType = token.TokenType,
            token = new { header, payload },
            key = token.DPoPKey is { } key
                ? new { publicJwk = JsonSerializer.Serialize(key.PublicJwk, Jwt.Indented), thumbprint = key.Thumbprint }
                : null,
            passkeysUrl
        });
    }
}
