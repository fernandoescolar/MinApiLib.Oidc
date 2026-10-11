namespace Samples.Server;

/// <summary>
/// A minimal HTML layout, to keep the sample free of a UI framework.
/// </summary>
public static class Html
{
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public static IResult Page(string title, string body, string? head = null) => Results.Content($$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{Encode(title)}} · Sample OIDC server</title>
          {{head}}
          <style>
            body { font-family: system-ui, sans-serif; max-width: 42rem; margin: 2rem auto; padding: 0 1rem; color: #1f2328; }
            h1 { font-size: 1.4rem; } label { display: block; margin: .8rem 0 .2rem; }
            input { padding: .4rem; width: 100%; box-sizing: border-box; }
            button { margin-top: 1rem; padding: .5rem 1rem; }
            .error { color: #b42318; } .ok { color: #1a7f37; } .muted { color: #59636e; } code { background: #f2f4f7; padding: 0 .2rem; }
            hr { margin: 1.5rem 0; border: 0; border-top: 1px solid #d0d7de; }
            table { width: 100%; border-collapse: collapse; margin: 1rem 0; } th, td { text-align: left; padding: .3rem; border-bottom: 1px solid #d0d7de; }
            td form button { margin: 0; padding: .2rem .6rem; }
          </style>
        </head>
        <body>
          <h1>{{Encode(title)}}</h1>
          {{body}}
        </body>
        </html>
        """, "text/html; charset=utf-8");
}
