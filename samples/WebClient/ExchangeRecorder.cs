namespace Samples.WebClient;

/// <summary>
/// Records the API requests, as they go out, and the responses, in the current request's items, so the page can show
/// them. It is not part of calling the API: only of showing it.
/// </summary>
public sealed class ExchangeRecorder(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string ItemName = "sample.exchanges";

    public sealed record Exchange(string Request, string? Proof, int Status, string? Challenge, string Body);

    public static IReadOnlyList<Exchange> Recorded(HttpContext http)
        => http.Items[ItemName] as List<Exchange> ?? [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (httpContextAccessor.HttpContext is { } http)
        {
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var proof = request.Headers.TryGetValues(DPoPProof.HeaderName, out var values) ? values.First() : null;
            var challenge = response.Headers.WwwAuthenticate.Count == 0 ? null : string.Join(", ", response.Headers.WwwAuthenticate);

            if (http.Items[ItemName] is not List<Exchange> exchanges)
            {
                http.Items[ItemName] = exchanges = [];
            }

            exchanges.Add(new Exchange(Describe(request), proof, (int)response.StatusCode, challenge, Api.Pretty(body)));
        }

        return response;
    }

    private static string Describe(HttpRequestMessage request)
    {
        var url = request.RequestUri!;
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"{request.Method} {url.PathAndQuery} HTTP/1.1\nHost: {url.Authority}\n");
        foreach (var header in request.Headers.Where(h => h.Key != "traceparent"))
        {
            text.Append(CultureInfo.InvariantCulture, $"{header.Key}: {Shorten(string.Join(", ", header.Value))}\n");
        }

        return text.ToString();
    }

    private static string Shorten(string value)
    {
        var space = value.IndexOf(' ', StringComparison.Ordinal);
        var token = space < 0 ? value : value[(space + 1)..];
        var shortened = token.Length > 48 ? token[..48] + "…" : token;
        return space < 0 ? shortened : value[..space] + " " + shortened;
    }
}
