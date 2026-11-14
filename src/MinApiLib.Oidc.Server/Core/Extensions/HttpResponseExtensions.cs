// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Derived from IdentityServer4 4.1.2 (https://github.com/IdentityServer/IdentityServer4); modified for MinApiLib.Oidc.

namespace MinApiLib.Oidc.Server.Core.Extensions;

public static class HttpResponseExtensions
{
    public static async Task WriteJsonAsync(this HttpResponse response, object o, string? contentType = null)
    {
        var json = ObjectSerializer.ToString(o);
        await response.WriteJsonAsync(json, contentType);
        await response.Body.FlushAsync();
    }

    public static async Task WriteJsonAsync(this HttpResponse response, string json, string? contentType = null)
    {
        response.ContentType = contentType ?? "application/json; charset=UTF-8";
        await response.WriteAsync(json);
        await response.Body.FlushAsync();
    }

    public static void SetCache(this HttpResponse response, int maxAge, params string[] varyBy)
    {
        if (maxAge == 0)
        {
            SetNoCache(response);
        }
        else if (maxAge > 0)
        {
            if (!response.Headers.ContainsKey("Cache-Control"))
            {
                response.Headers.Append("Cache-Control", $"max-age={maxAge}");
            }

            if (varyBy?.Length > 0)
            {
                var vary = varyBy.Aggregate((x, y) => x + "," + y);
                if (response.Headers.TryGetValue("Vary", out var existing))
                {
                    vary = existing.ToString() + "," + vary;
                }
                response.Headers["Vary"] = vary;
            }
        }
    }

    public static void SetNoCache(this HttpResponse response)
    {
        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers.Append("Cache-Control", "no-store, no-cache, max-age=0");
        }
        else
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        }

        if (!response.Headers.ContainsKey("Pragma"))
        {
            response.Headers.Append("Pragma", "no-cache");
        }
    }

    public static async Task WriteHtmlAsync(this HttpResponse response, string html)
    {
        response.ContentType = "text/html; charset=UTF-8";
        await response.WriteAsync(html, Encoding.UTF8);
        await response.Body.FlushAsync();
    }

    public static void RedirectToAbsoluteUrl(this HttpResponse response, string url)
    {
        if (url.IsLocalUrl())
        {
            if (url.StartsWith("~/", StringComparison.Ordinal)) url = url.Substring(1);
            url = response.HttpContext.GetOidcServerBaseUrl().EnsureTrailingSlash() + url.RemoveLeadingSlash();
        }
        response.RedirectSeeOtherAfterPost(url);
    }

    /// <summary>
    /// Redirects to <paramref name="url"/>. A POST request is answered with 303 See Other so the browser
    /// follows with GET and never re-submits the form body to the target (FAPI 2.0 section 5.3.2.2).
    /// Other methods keep the 302 status.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="url">The redirect target.</param>
    public static void RedirectSeeOtherAfterPost(this HttpResponse response, string url)
    {
        response.Redirect(url);
        if (HttpMethods.IsPost(response.HttpContext.Request.Method))
        {
            response.StatusCode = StatusCodes.Status303SeeOther;
        }
    }

    public static void AddScriptCspHeaders(this HttpResponse response, CspOptions options, string hash)
    {
        var csp1part = options.Level == CspLevel.One ? "'unsafe-inline' " : string.Empty;
        var cspHeader = $"default-src 'none'; script-src {csp1part}'{hash}'";

        AddCspHeaders(response.Headers, options, cspHeader);
    }

    public static void AddStyleCspHeaders(this HttpResponse response, CspOptions options, string hash, string? frameSources)
    {
        var csp1part = options.Level == CspLevel.One ? "'unsafe-inline' " : string.Empty;
        var cspHeader = $"default-src 'none'; style-src {csp1part}'{hash}'";

        if (!string.IsNullOrEmpty(frameSources))
        {
            cspHeader += $"; frame-src {frameSources}";
        }

        AddCspHeaders(response.Headers, options, cspHeader);
    }

    public static void AddCspHeaders(IHeaderDictionary headers, CspOptions options, string cspHeader)
    {
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers.Append("Content-Security-Policy", cspHeader);
        }
        if (options.AddDeprecatedHeader && !headers.ContainsKey("X-Content-Security-Policy"))
        {
            headers.Append("X-Content-Security-Policy", cspHeader);
        }
    }
}
