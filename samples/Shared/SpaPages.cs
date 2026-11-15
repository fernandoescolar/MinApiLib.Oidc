using System.Net;
using System.Text.Json;

namespace Samples;

/// <summary>
/// Renders the pages of the sample's frontend (ClientApp/, React built by Vite into wwwroot/). Every page is the
/// same index: <c>&lt;div id="app" app-id="..." app-model="..."&gt;</c>, which tells the frontend which page to mount
/// and gives it its model as base64 UTF-8 JSON, and the script and style of the build, read from Vite's manifest.
/// </summary>
public sealed class SpaPages
{
    private const string Entry = "src/main.tsx";
    private static readonly JsonSerializerOptions ModelJson = new(JsonSerializerDefaults.Web);

    private readonly string _manifestPath;
    private readonly string _titleSuffix;
    private readonly Lock _lock = new();
    private (DateTime Written, Assets Assets)? _cache;

    public SpaPages(IWebHostEnvironment environment, string titleSuffix)
    {
        _manifestPath = Path.Combine(environment.ContentRootPath, "wwwroot", ".vite", "manifest.json");
        _titleSuffix = titleSuffix;
    }

    /// <summary>The index of the page <paramref name="appId"/>, with <paramref name="model"/> as its model.</summary>
    public IResult Render(string appId, string title, object? model = null)
    {
        var assets = GetAssets();
        var encodedModel = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(model ?? new { }, ModelJson));
        var styles = string.Concat(assets.Styles.Select(href => $"  <link rel=\"stylesheet\" href=\"/{Encode(href)}\">\n"));

        return Results.Content($$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{Encode(title)}} · {{Encode(_titleSuffix)}}</title>
            {{styles}}</head>
            <body>
              <div id="app" app-id="{{Encode(appId)}}" app-model="{{encodedModel}}"></div>
              <script type="module" src="/{{Encode(assets.Script)}}"></script>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }

    // The file names carry a content hash, so the manifest is read again when a new build (npm run watch) writes it.
    private Assets GetAssets()
    {
        var written = File.Exists(_manifestPath)
            ? File.GetLastWriteTimeUtc(_manifestPath)
            : throw new InvalidOperationException($"The frontend is not built: {_manifestPath} is missing. Build the project, or run 'npm run build' in ClientApp.");

        lock (_lock)
        {
            if (_cache is { } cache && cache.Written == written)
            {
                return cache.Assets;
            }

            using var manifest = JsonDocument.Parse(File.ReadAllBytes(_manifestPath));
            var entry = manifest.RootElement.GetProperty(Entry);
            var assets = new Assets(
                entry.GetProperty("file").GetString()!,
                entry.TryGetProperty("css", out var css) ? [.. css.EnumerateArray().Select(c => c.GetString()!)] : []);
            _cache = (written, assets);
            return assets;
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private sealed record Assets(string Script, string[] Styles);
}
