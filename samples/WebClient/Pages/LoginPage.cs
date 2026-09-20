namespace Samples.WebClient.Pages;

/// <summary>
/// Starts the sign-in with one of the two OpenID Connect schemes.
/// </summary>
public sealed record LoginPage() : GetHandler<LoginPage.Request>("/login")
{
    public readonly record struct Request([FromQuery] string? Scheme);

    protected override IResult Handle(Request request)
    {
        var scheme = request.Scheme == Schemes.DPoP ? Schemes.DPoP : Schemes.Bearer;
        return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [scheme]);
    }
}
