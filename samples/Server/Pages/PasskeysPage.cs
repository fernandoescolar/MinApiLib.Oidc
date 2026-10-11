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
        [FromServices] IAntiforgery Antiforgery);

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

        var tokens = request.Antiforgery.GetAndStoreTokens(http);
        var antiforgeryField = $"""<input type="hidden" name="{Html.Encode(tokens.FormFieldName)}" value="{Html.Encode(tokens.RequestToken)}">""";
        var passkeys = await request.Users.GetPasskeysAsync(user);

        var rows = passkeys.Count == 0
            ? """<tr><td colspan="4" class="muted">No passkeys yet.</td></tr>"""
            : string.Concat(passkeys.Select(p => $"""
                <tr>
                  <td>{Html.Encode(p.Name)}</td>
                  <td>{p.CreatedAt:yyyy-MM-dd HH:mm}</td>
                  <td>{(p.IsBackedUp ? "Synced (swk)" : "This device (hwk)")}, {(p.IsUserVerified ? "user verified (mfa)" : "user present")}</td>
                  <td>
                    <form method="post" action="/account/passkey/remove">
                      {antiforgeryField}
                      <input type="hidden" name="id" value="{Base64Url.EncodeToString(p.CredentialId)}">
                      <button type="submit">Remove</button>
                    </form>
                  </td>
                </tr>
                """));

        var notice = request.Error != null
            ? $"""<p class="error">{Html.Encode(request.Error)}</p>"""
            : request.Message != null ? $"""<p class="ok">{Html.Encode(request.Message)}</p>""" : string.Empty;

        return Html.Page($"Passkeys of {user.Name}", $"""
            {notice}
            <p>A passkey signs you in without a password, and cannot be phished: the browser only uses it on this server.
               The tokens then say how you signed in: <code>amr</code> and <code>"acr": "phr"</code>.</p>
            <table>
              <tr><th>Name</th><th>Created</th><th>Kind</th><th></th></tr>
              {rows}
            </table>
            <form id="add-passkey-form" method="post" action="/account/passkey/register">
              {antiforgeryField}
              <input type="hidden" name="credential">
              <label for="name">Name of the new passkey</label>
              <input id="name" name="name" placeholder="My laptop">
              <button type="button" id="add-passkey-button">Add a passkey</button>
              <p id="passkey-status" class="muted"></p>
            </form>
            <p class="muted">The sample keeps passkeys in memory: they are gone when the server restarts, so remove them from
               your browser or password manager too.</p>
            {PasskeyScripts.Register}
            """);
    }
}
