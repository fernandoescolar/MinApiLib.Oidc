using System.Text;
using System.Text.Json;

namespace Samples.Worker;

/// <summary>
/// Prints each API request as it goes out (after the token handler) and the response.
/// </summary>
public sealed class ConsolePrinter : DelegatingHandler
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine($"{request.Method} {request.RequestUri!.PathAndQuery} HTTP/1.1");
        Console.WriteLine($"Host: {request.RequestUri.Authority}");
        Console.WriteLine($"Authorization: {request.Headers.Authorization?.Scheme} {Shorten(request.Headers.Authorization?.Parameter)}");
        if (request.Headers.TryGetValues("DPoP", out var proofs))
        {
            var proof = proofs.First();
            Console.WriteLine($"DPoP: {Shorten(proof)}");
            Console.WriteLine($"  proof header:  {Decode(proof, 0)}");
            Console.WriteLine($"  proof payload: {Decode(proof, 1)}");
        }

        var response = await base.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine();
        Console.WriteLine($"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine(Pretty(body));
        return response;
    }

    private static string Shorten(string? value) => value == null ? string.Empty : value.Length > 40 ? value[..40] + "…" : value;

    private static string Decode(string jwt, int part)
    {
        var segment = jwt.Split('.')[part].Replace('-', '+').Replace('_', '/');
        segment = segment.PadRight(segment.Length + ((4 - (segment.Length % 4)) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(segment));
    }

    private static string Pretty(string json)
    {
        try
        {
            return JsonSerializer.Serialize(JsonElement.Parse(json), Indented);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
