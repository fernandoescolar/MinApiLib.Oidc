namespace Samples.Server;

/// <summary>
/// The antiforgery token of a page's forms, in its model: the frontend writes it as a hidden field, and the form-bound
/// endpoints check it.
/// </summary>
public sealed record AntiforgeryModel(string FieldName, string Token)
{
    public static AntiforgeryModel For(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        return new AntiforgeryModel(tokens.FormFieldName, tokens.RequestToken!);
    }
}
