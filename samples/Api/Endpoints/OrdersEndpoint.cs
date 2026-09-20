namespace Samples.Api.Endpoints;

/// <summary>
/// The protected resource: the orders of the user, and how the request was authenticated.
/// </summary>
public sealed record OrdersEndpoint() : GetHandler<OrdersEndpoint.Request>("/orders")
{
    public readonly record struct Request(HttpContext HttpContext);

    protected override RouteHandlerBuilder Configure(RouteHandlerBuilder builder)
        => builder.RequireAuthorization("orders.read");

    protected override IResult Handle(Request request)
    {
        var http = request.HttpContext;
        var user = http.User;
        return Results.Ok(new
        {
            user = user.FindFirst("sub")?.Value,
            name = user.FindFirst("name")?.Value,
            client = user.FindFirst("client_id")?.Value,
            tokenScheme = http.Items[DPoPJwtBearerEvents.SchemeItem],
            boundToKey = http.Items[DPoPJwtBearerEvents.ThumbprintItem],
            orders = new[]
            {
                new { id = 1001, total = 42.50m, status = "shipped" },
                new { id = 1002, total = 18.00m, status = "processing" }
            }
        });
    }
}
