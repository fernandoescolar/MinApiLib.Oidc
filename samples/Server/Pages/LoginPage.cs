namespace Samples.Server.Pages;

/// <summary>
/// The login page. The authorize endpoint redirects here with the authorize request in <c>returnUrl</c>.
/// </summary>
public sealed record LoginPage() : GetHandlerAsync<LoginPage.Request>("/account/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? ReturnUrl,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        return Render(request.HttpContext, request.Antiforgery, request.ReturnUrl, context?.Client.ClientName, error: null);
    }

    public static IResult Render(HttpContext http, IAntiforgery antiforgery, string? returnUrl, string? clientName, string? error)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        var errorHtml = error == null ? string.Empty : $"""<p class="error">{Html.Encode(error)}</p>""";
        var client = clientName == null ? string.Empty : $"""<p class="muted">Signing in to <strong>{Html.Encode(clientName)}</strong>.</p>""";

        return Html.Page("Sign in", $"""
            {client}
            {errorHtml}
            <form method="post" action="/account/login">
              <input type="hidden" name="{Html.Encode(tokens.FormFieldName)}" value="{Html.Encode(tokens.RequestToken)}">
              <input type="hidden" name="returnUrl" value="{Html.Encode(returnUrl)}">
              <label for="username">User name</label>
              <input id="username" name="username" autocomplete="username" autofocus>
              <label for="password">Password</label>
              <input id="password" name="password" type="password" autocomplete="current-password">
              <button type="submit">Sign in</button>
            </form>
            <p class="muted">Users: <code>alice</code> / <code>alice</code> and <code>bob</code> / <code>bob</code>.</p>
            """);
    }
}

/// <summary>
/// Checks the credentials, signs the user in to the server's cookie and returns to the authorize request.
/// Binding the form turns on antiforgery validation.
/// </summary>
public sealed record LoginForm() : PostHandlerAsync<LoginForm.Request>("/account/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Username,
        [FromForm] string? Password,
        [FromForm] string? ReturnUrl,
        [FromServices] UserStore Users,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);

        var user = request.Users.Validate(request.Username, request.Password);
        if (user == null)
        {
            return LoginPage.Render(request.HttpContext, request.Antiforgery, request.ReturnUrl, context?.Client.ClientName, "Invalid user name or password.");
        }

        await request.HttpContext.SignInAsync(new OidcServerUser(user.SubjectId)
        {
            DisplayName = user.Name,
            AdditionalClaims = { new Claim("name", user.Name), new Claim("email", user.Email) }
        });

        // Only return to a valid authorize request: anything else could be an open redirect.
        return context != null ? Results.Redirect(request.ReturnUrl!) : Results.Redirect("/");
    }
}
