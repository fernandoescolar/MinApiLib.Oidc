namespace IdP.Infrastructure;

/// <summary>
/// The antiforgery token of a page: the flow pages write it as a hidden field of their forms, and the consoles send it
/// in the <see cref="HeaderName"/> header of their API calls.
/// </summary>
public sealed record AntiforgeryModel(string FieldName, string HeaderName, string Token)
{
    public const string Header = "X-XSRF-TOKEN";

    public static AntiforgeryModel For(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        return new AntiforgeryModel(tokens.FormFieldName, Header, tokens.RequestToken!);
    }
}
