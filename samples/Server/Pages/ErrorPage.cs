namespace Samples.Server.Pages;

/// <summary>
/// Shows protocol errors that cannot be sent back to the client, such as an unknown redirect URI.
/// </summary>
public sealed record ErrorPage() : GetHandlerAsync<ErrorPage.Request>("/home/error")
{
    public readonly record struct Request(
        [FromQuery] string? ErrorId,
        [FromServices] IOidcServerInteractionService Interaction,
        [FromServices] SpaPages Spa);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var error = request.ErrorId == null ? null : await request.Interaction.GetErrorContextAsync(request.ErrorId);

        return request.Spa.Render("error", "Error", new
        {
            error = error?.Error,
            errorDescription = error?.ErrorDescription,
            requestId = error?.RequestId
        });
    }
}
