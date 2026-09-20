namespace Samples.Api.Endpoints;

public sealed record RootEndpoint() : GetHandler("/")
{
    private static readonly string[] Endpoints = ["GET /orders (scope orders.read; Bearer or DPoP)"];

    protected override IResult Handle() => Results.Ok(new { name = "Orders API", endpoints = Endpoints });
}
