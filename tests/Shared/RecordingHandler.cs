using System.Collections.Concurrent;

namespace MinApiLib.Oidc.Testing;

public sealed record RecordedRequest(HttpMethod Method, Uri Url, string? Authorization, string? DPoP, string? Body)
{
    public string Path => Url.AbsolutePath;

    public string? Form(string name)
    {
        if (Body == null)
        {
            return null;
        }

        foreach (var pair in Body.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0].Replace('+', ' ')) == name)
            {
                return parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            }
        }

        return null;
    }
}

/// <summary>
/// Records the requests that go through it: the URL, the Authorization and DPoP headers, and the body.
/// </summary>
public sealed class RequestLog
{
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    public IReadOnlyList<RecordedRequest> Requests => [.. _requests];

    public void Add(RecordedRequest request) => _requests.Enqueue(request);

    public IReadOnlyList<RecordedRequest> To(string path) => Requests.Where(r => r.Path == path).ToList();
}

public sealed class RecordingHandler(RequestLog log) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = null;
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        log.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues("DPoP", out var dpop) ? dpop.Single() : null,
            body));

        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Answers requests with a function, and records them.
/// </summary>
public sealed class StubHandler(Func<RecordedRequest, int, HttpResponseMessage> respond) : HttpMessageHandler
{
    private int _calls;

    public RequestLog Log { get; } = new();

    public TimeSpan Delay { get; init; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization?.ToString(),
            request.Headers.TryGetValues("DPoP", out var dpop) ? dpop.Single() : null,
            body);
        Log.Add(recorded);

        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        return respond(recorded, Interlocked.Increment(ref _calls) - 1);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, object body, params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }
}

/// <summary>
/// Gives HttpClientFactory a new handler each time: it disposes the handlers it rotates out.
/// </summary>
public static class Handlers
{
    public static HttpMessageHandler Recording(RequestLog log, HttpMessageHandler inner) => new RecordingHandler(log) { InnerHandler = inner };

    public static HttpMessageHandler NonDisposing(HttpMessageHandler inner) => new Forwarding(inner);

    private sealed class Forwarding(HttpMessageHandler inner) : HttpMessageHandler
    {
        // The inner handler is shared by the test: it is not disposed with this one.
        private readonly HttpMessageInvoker _inner = new(inner, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _inner.SendAsync(request, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
