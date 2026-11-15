namespace Samples.Server.Pages;

/// <summary>
/// The end session endpoint redirects here. The page signs the user out and loads the front-channel logout iframe,
/// which signs the user out of every client of the session, then offers the way back to the client.
/// </summary>
public sealed record LogoutPage() : GetHandlerAsync<LogoutPage.Request>("/account/logout")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? LogoutId,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var logout = await request.Interaction.GetLogoutContextAsync(request.LogoutId!);
        await request.HttpContext.SignOutAsync();

        return request.Spa.Render("logout", "Signed out", new
        {
            postLogoutRedirectUri = logout.PostLogoutRedirectUri,
            clientName = logout.ClientName ?? logout.ClientId,
            signOutIFrameUrl = logout.SignOutIFrameUrl
        });
    }
}
