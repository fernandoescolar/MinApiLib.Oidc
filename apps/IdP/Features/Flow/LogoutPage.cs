namespace IdP.Features.Flow;

/// <summary>
/// The end session endpoint redirects here. When the request came from an authenticated client with an
/// <c>id_token_hint</c>, the user is signed out at once; otherwise the page asks first. Then it loads the front-channel
/// logout iframe, which signs the user out of every client of the session, and offers the way back to the client.
/// </summary>
public sealed record LogoutPage() : GetHandlerAsync<LogoutPage.Request>("/logout")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? LogoutId,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IEventService Events,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var logout = await request.Interaction.GetLogoutContextAsync(request.LogoutId!);

        if (http.User.Identity?.IsAuthenticated == true && logout.ShowSignoutPrompt)
        {
            return request.Spa.Render("logout", "Sign out", new
            {
                prompt = true,
                logoutId = request.LogoutId,
                antiforgery = AntiforgeryModel.For(http, request.Antiforgery),
                product = FlowPages.Product(request.Options.Value)
            });
        }

        return await LogoutForm.SignOutAsync(http, request.LogoutId, request.Interaction, request.Events, request.Options.Value, request.Spa);
    }
}

public sealed record LogoutForm() : PostHandlerAsync<LogoutForm.Request>("/logout")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromForm] string? LogoutId,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] IEventService Events,
        [FromServices] IOptions<IdPOptions> Options,
        [FromServices] SpaPages Spa);

    protected override Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
        => SignOutAsync(request.HttpContext, request.LogoutId, request.Interaction, request.Events, request.Options.Value, request.Spa);

    public static async Task<IResult> SignOutAsync(HttpContext http, string? logoutId, IOidcServerInteractionService interaction, IEventService events, IdPOptions options, SpaPages spa)
    {
        if (http.User.Identity?.IsAuthenticated == true)
        {
            // Without a logout id, create one now: it captures the clients of the session before the cookie goes.
            logoutId ??= await interaction.CreateLogoutContextAsync();
            await events.RaiseAsync(new UserLogoutSuccessEvent(http.User.GetSubjectId(), http.User.FindFirst(JwtClaimTypes.Name)?.Value ?? string.Empty));
            await http.SignOutAsync();
        }

        var logout = await interaction.GetLogoutContextAsync(logoutId!);
        return spa.Render("logout", "Signed out", new
        {
            prompt = false,
            postLogoutRedirectUri = logout.PostLogoutRedirectUri,
            clientName = logout.ClientName ?? logout.ClientId,
            signOutIFrameUrl = logout.SignOutIFrameUrl,
            product = FlowPages.Product(options)
        });
    }
}
