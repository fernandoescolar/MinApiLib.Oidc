namespace Samples.Server.Pages;

/// <summary>
/// Shows protocol errors that cannot be sent back to the client, such as an unknown redirect URI.
/// </summary>
public sealed record ErrorPage() : GetHandlerAsync<ErrorPage.Request>("/home/error")
{
    public readonly record struct Request(
        [FromQuery] string? ErrorId,
        [FromServices] IOidcServerInteractionService Interaction);

    protected override async Task<IResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var error = request.ErrorId == null ? null : await request.Interaction.GetErrorContextAsync(request.ErrorId);

        return Html.Page("Error", $"""
            <p class="error"><strong>{Html.Encode(error?.Error ?? "unknown_error")}</strong></p>
            <p>{Html.Encode(error?.ErrorDescription)}</p>
            <p class="muted">Request id: {Html.Encode(error?.RequestId)}</p>
            """);
    }
}
