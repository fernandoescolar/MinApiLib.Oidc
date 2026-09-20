namespace Samples.WebClient;

public static class Html
{
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public static IResult Page(string title, string body) => Results.Content($$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{Encode(title)}} · Sample web client</title>
          <style>
            body { font-family: system-ui, sans-serif; max-width: 56rem; margin: 2rem auto; padding: 0 1rem; color: #1f2328; }
            h1 { font-size: 1.4rem; } h2 { font-size: 1.1rem; margin-top: 2rem; }
            pre { background: #f6f8fa; padding: .8rem; overflow-x: auto; font-size: .85rem; }
            .actions a, .actions button { display: inline-block; margin: .2rem .4rem .2rem 0; padding: .45rem .9rem;
              border: 1px solid #d0d7de; border-radius: 6px; text-decoration: none; color: inherit; background: #f6f8fa; }
            .ok { color: #1a7f37; } .fail { color: #b42318; } .muted { color: #59636e; }
          </style>
        </head>
        <body>
          <h1>{{Encode(title)}}</h1>
          {{body}}
        </body>
        </html>
        """, "text/html; charset=utf-8");

    public static string Pre(string? text) => $"<pre>{Encode(text)}</pre>";
}
