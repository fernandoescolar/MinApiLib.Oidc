namespace Samples.WebClient.Pages;

public sealed record HomePage() : GetHandlerAsync<HomePage.Request>("/")
{
    public readonly record struct Request(HttpContext HttpContext, [FromServices] IUserAccessTokenManager Tokens);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var result = await http.AuthenticateAsync(Schemes.Cookie);
        if (!result.Succeeded)
        {
            return Html.Page("Sample web client", """
                <p>Sign in with the sample server. Both buttons sign in the same user; they use two clients of the server:</p>
                <ul>
                  <li><strong>Bearer</strong>: the client <code>web</code> gets a plain bearer access token.</li>
                  <li><strong>DPoP</strong>: the client <code>web.dpop</code> proves possession of a key it creates for the session,
                      and gets an access token bound to that key.</li>
                </ul>
                <p class="actions"><a href="/login?scheme=oidc">Sign in (Bearer)</a><a href="/login?scheme=oidc-dpop">Sign in (DPoP)</a></p>
                <p class="muted">Users: alice / alice and bob / bob.</p>
                """);
        }

        // The token the API calls use: the library reads it from the session, refreshed if it is about to expire.
        var token = (await request.Tokens.GetAccessTokenAsync(http, Schemes.Cookie, cancellationToken: cancellationToken))!;
        var tokenType = token.TokenType;
        var (header, payload) = Jwt.Decode(token.Value);

        var keySection = string.Empty;
        if (token.DPoPKey is { } key)
        {
            keySection = $"""
                <h2>Session key</h2>
                <p>The public key, sent in every proof, and its thumbprint, which the access token carries in <code>cnf.jkt</code>.
                   The authorization request sent the thumbprint too (<code>dpop_jkt</code>), which bound the code to the key:</p>
                {Html.Pre(JsonSerializer.Serialize(key.PublicJwk, Jwt.Indented))}
                {Html.Pre("jkt = " + key.Thumbprint)}
                """;
        }

        var modes = tokenType.Equals("DPoP", StringComparison.OrdinalIgnoreCase)
            ? """
              <a href="/call-api">Call the API with a proof</a>
              <a href="/call-api?mode=bearer">Send the token as a bearer token</a>
              <a href="/call-api?mode=other-key">Sign the proof with another key</a>
              <a href="/call-api?mode=replay">Replay the proof</a>
              """
            : """<a href="/call-api">Call the API</a>""";

        return Html.Page($"Signed in as {result.Principal!.Identity!.Name}", $"""
            <p>Signed in with the <strong>{Html.Encode(Schemes.Of(result.Properties))}</strong> scheme.
               The token endpoint answered <code>"token_type": "{Html.Encode(tokenType)}"</code>.</p>
            <p class="actions">{modes}<a href="/logout">Sign out</a></p>
            <h2>Access token</h2>
            {Html.Pre(header)}
            {Html.Pre(payload)}
            {keySection}
            """);
    }
}
