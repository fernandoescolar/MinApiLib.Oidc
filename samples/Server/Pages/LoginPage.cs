namespace Samples.Server.Pages;

/// <summary>
/// The login page. The authorize endpoint redirects here with the authorize request in <c>returnUrl</c>. The user
/// signs in with a password or a passkey; when the client asks for <c>acr_values=phr</c>, only with a passkey.
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
        return Render(request.HttpContext, request.Antiforgery, request.ReturnUrl, context, error: null);
    }

    public static IResult Render(HttpContext http, IAntiforgery antiforgery, string? returnUrl, AuthorizationRequest? context, string? error)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        var antiforgeryField = $"""<input type="hidden" name="{Html.Encode(tokens.FormFieldName)}" value="{Html.Encode(tokens.RequestToken)}">""";
        var returnUrlField = $"""<input type="hidden" name="returnUrl" value="{Html.Encode(returnUrl)}">""";
        var errorHtml = error == null ? string.Empty : $"""<p class="error">{Html.Encode(error)}</p>""";
        var client = context == null ? string.Empty : $"""<p class="muted">Signing in to <strong>{Html.Encode(context.Client.ClientName ?? context.Client.ClientId)}</strong>.</p>""";

        // The passkey form: the script fills in the browser's answer and submits it.
        var passkey = $"""
            <form id="passkey-form" method="post" action="/account/passkey/login">
              {antiforgeryField}
              {returnUrlField}
              <input type="hidden" name="credential">
              <button type="button" id="passkey-button">Sign in with a passkey</button>
              <p id="passkey-status" class="muted"></p>
            </form>
            """;

        if (Login.RequiresPasskey(context))
        {
            return Html.Page("Sign in", $"""
                {client}
                <p>This application asks for a phishing-resistant sign-in (<code>acr_values=phr</code>): use a passkey.</p>
                {errorHtml}
                {passkey}
                <p class="muted">No passkey yet? Sign in with a password on <a href="/account/passkeys">your passkeys page</a> and add one.</p>
                {PasskeyScripts.SignIn}
                """);
        }

        // autocomplete="username webauthn" lets the browser offer the user's passkeys as they type (conditional UI).
        return Html.Page("Sign in", $"""
            {client}
            {errorHtml}
            <form method="post" action="/account/login">
              {antiforgeryField}
              {returnUrlField}
              <label for="username">User name</label>
              <input id="username" name="username" autocomplete="username webauthn" autofocus>
              <label for="password">Password</label>
              <input id="password" name="password" type="password" autocomplete="current-password">
              <button type="submit">Sign in</button>
            </form>
            <hr>
            {passkey}
            <p class="muted">Users: <code>alice</code> / <code>alice</code> and <code>bob</code> / <code>bob</code>. Add a passkey on
               <a href="/account/passkeys">your passkeys page</a>.</p>
            {PasskeyScripts.SignIn}
            """);
    }
}

/// <summary>
/// Checks the password with ASP.NET Core Identity, signs the user in to the server's cookie and returns to the
/// authorize request. Binding the form turns on antiforgery validation.
/// </summary>
public sealed record LoginForm() : PostHandlerAsync<LoginForm.Request>("/account/login")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? Username,
        [FromForm] string? Password,
        [FromForm] string? ReturnUrl,
        [FromServices] UserManager<SampleUser> Users,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IAntiforgery Antiforgery);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        if (Login.RequiresPasskey(context))
        {
            return LoginPage.Render(request.HttpContext, request.Antiforgery, request.ReturnUrl, context, "This application needs a passkey.");
        }

        var user = request.Username == null ? null : await request.Users.FindByNameAsync(request.Username);
        if (user == null || request.Password == null || !await request.Users.CheckPasswordAsync(user, request.Password))
        {
            return LoginPage.Render(request.HttpContext, request.Antiforgery, request.ReturnUrl, context, "Invalid user name or password.");
        }

        await Login.SignInWithPasswordAsync(request.HttpContext, user);
        return Login.Return(request.ReturnUrl, context);
    }
}
