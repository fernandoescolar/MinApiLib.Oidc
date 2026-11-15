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
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        return Render(request.HttpContext, request.Spa, request.Antiforgery, request.ReturnUrl, context, error: null);
    }

    public static IResult Render(HttpContext http, SpaPages spa, IAntiforgery antiforgery, string? returnUrl, AuthorizationRequest? context, string? error)
        => spa.Render("login", "Sign in", new
        {
            returnUrl,
            clientName = context == null ? null : context.Client.ClientName ?? context.Client.ClientId,
            requiresPasskey = Login.RequiresPasskey(context),
            error,
            antiforgery = AntiforgeryModel.For(http, antiforgery)
        });
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
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var context = request.ReturnUrl == null ? null : await request.Interaction.GetAuthorizationContextAsync(request.ReturnUrl);
        if (Login.RequiresPasskey(context))
        {
            return LoginPage.Render(request.HttpContext, request.Spa, request.Antiforgery, request.ReturnUrl, context, "This application needs a passkey.");
        }

        var user = request.Username == null ? null : await request.Users.FindByNameAsync(request.Username);
        if (user == null || request.Password == null || !await request.Users.CheckPasswordAsync(user, request.Password))
        {
            return LoginPage.Render(request.HttpContext, request.Spa, request.Antiforgery, request.ReturnUrl, context, "Invalid user name or password.");
        }

        await Login.SignInWithPasswordAsync(request.HttpContext, user);
        return Login.Return(request.ReturnUrl, context);
    }
}
