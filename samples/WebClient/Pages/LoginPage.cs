namespace Samples.WebClient.Pages;

/// <summary>
/// Starts the sign-in with one of the two OpenID Connect schemes. <c>acr=phr</c> asks the server for a
/// phishing-resistant sign-in: a passkey.
/// </summary>
public sealed record LoginPage() : GetHandler<LoginPage.Request>("/login")
{
    public readonly record struct Request([FromQuery] string? Scheme, [FromQuery] string? Acr);

    protected override IResult Handle(Request request)
    {
        var scheme = request.Scheme == Schemes.DPoP ? Schemes.DPoP : Schemes.Bearer;
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        if (request.Acr == Oidc.PhishingResistant)
        {
            properties.Items[Oidc.AcrValuesProperty] = Oidc.PhishingResistant;
        }

        return Results.Challenge(properties, [scheme]);
    }
}
