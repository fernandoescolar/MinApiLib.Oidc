namespace Samples.WebClient.Pages;

/// <summary>
/// Signs out of the web application and of the server (RP-initiated logout with the scheme that signed in).
/// </summary>
public sealed record LogoutPage() : GetHandlerAsync<LogoutPage.Request>("/logout")
{
    public readonly record struct Request(HttpContext HttpContext);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var result = await request.HttpContext.AuthenticateAsync(Schemes.Cookie);
        var scheme = result.Properties?.Items.TryGetValue(Schemes.SchemeProperty, out var value) == true && value != null ? value : Schemes.Bearer;

        return Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [Schemes.Cookie, scheme]);
    }
}
