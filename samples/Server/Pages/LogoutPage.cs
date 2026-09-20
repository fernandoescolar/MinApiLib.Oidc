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
        [FromServices] IOidcServerInteractionService Interaction);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var logout = await request.Interaction.GetLogoutContextAsync(request.LogoutId!);
        await request.HttpContext.SignOutAsync();

        var iframe = logout.SignOutIFrameUrl == null
            ? string.Empty
            : $"""<iframe src="{Html.Encode(logout.SignOutIFrameUrl)}" width="0" height="0" style="display:none"></iframe>""";
        var back = logout.PostLogoutRedirectUri == null
            ? string.Empty
            : $"""<p><a href="{Html.Encode(logout.PostLogoutRedirectUri)}">Return to {Html.Encode(logout.ClientName ?? logout.ClientId)}</a></p>""";

        return Html.Page("Signed out", $"""
            <p>You are signed out.</p>
            {back}
            {iframe}
            """);
    }
}
