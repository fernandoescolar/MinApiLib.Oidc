namespace Samples.Server.Pages;

/// <summary>
/// The signed-in user's passkeys: add one, see how it was registered, remove it.
/// </summary>
public sealed record PasskeysPage() : GetHandlerAsync<PasskeysPage.Request>("/account/passkeys")
{
    public readonly record struct Request(
        HttpContext HttpContext,
        [FromQuery] string? Message,
        [FromQuery] string? Error,
        [FromServices] UserManager<SampleUser> Users,
        [FromServices] IAntiforgery Antiforgery,
        [FromServices] SpaPages Spa);

    public static async Task<SampleUser?> CurrentUserAsync(HttpContext http, UserManager<SampleUser> users)
        => http.User.Identity?.IsAuthenticated == true ? await users.FindByIdAsync(http.User.GetSubjectId()) : null;

    public static IResult Redirect(string? error = null, string? message = null)
    {
        var query = error != null ? QueryString.Create("error", error) : message != null ? QueryString.Create("message", message) : QueryString.Empty;
        return Results.Redirect("/account/passkeys" + query.ToUriComponent());
    }

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var http = request.HttpContext;
        var user = await CurrentUserAsync(http, request.Users);
        if (user == null)
        {
            return Results.Redirect("/account/login?returnUrl=" + Uri.EscapeDataString("/account/passkeys"));
        }

        var passkeys = await request.Users.GetPasskeysAsync(user);

        return request.Spa.Render("passkeys", $"Passkeys of {user.Name}", new
        {
            userName = user.Name,
            message = request.Message,
            error = request.Error,
            passkeys = passkeys.Select(p => new
            {
                id = Base64Url.EncodeToString(p.CredentialId),
                name = p.Name,
                createdAt = p.CreatedAt,
                synced = p.IsBackedUp,
                userVerified = p.IsUserVerified
            }),
            antiforgery = AntiforgeryModel.For(http, request.Antiforgery)
        });
    }
}
