namespace Samples.Server.Pages;

public sealed record HomePage() : GetHandler<HomePage.Request>("/")
{
    public readonly record struct Request([FromServices] SpaPages Spa);

    protected override IResult Handle(Request request) => request.Spa.Render("home", "Home");
}
