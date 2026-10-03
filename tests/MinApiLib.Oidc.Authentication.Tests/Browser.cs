using System.Text.RegularExpressions;

namespace MinApiLib.Oidc.Authentication.Tests;

/// <summary>
/// A browser for the test hosts: keeps cookies, follows redirects and submits the form_post page of the server.
/// </summary>
public sealed partial class Browser(TestOidcServer server, TestWebApp web) : IDisposable
{
    private readonly CookieContainer _cookies = new();
    private readonly HttpMessageInvoker _server = new(server.CreateHandler());
    private readonly HttpMessageInvoker _web = new(web.CreateHandler());

    /// <summary>The URLs the browser navigated to.</summary>
    public List<Uri> History { get; } = [];

    public Task<HttpResponseMessage> GetAsync(string url) => NavigateAsync(new HttpRequestMessage(HttpMethod.Get, url));

    /// <summary>Signs in with an OpenID Connect scheme of the web application, as "bob".</summary>
    public async Task SignInAsync(string scheme)
    {
        using var authorizeResponse = await GetAsync($"{TestWebApp.Url}/login?scheme={scheme}");
        var page = await authorizeResponse.Content.ReadAsStringAsync();
        var action = WebUtility.HtmlDecode(FormAction().Match(page).Groups[1].Value);
        Assert.StartsWith(TestWebApp.Url + "/signin-oidc", action, StringComparison.Ordinal);

        var fields = HiddenInput().Matches(page).ToDictionary(m => WebUtility.HtmlDecode(m.Groups[1].Value), m => WebUtility.HtmlDecode(m.Groups[2].Value));
        using var done = await NavigateAsync(new HttpRequestMessage(HttpMethod.Post, action) { Content = new FormUrlEncodedContent(fields) });

        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal("signed in", await done.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> NavigateAsync(HttpRequestMessage request)
    {
        for (var hops = 0; hops < 20; hops++)
        {
            var url = request.RequestUri!;
            History.Add(url);
            var cookie = _cookies.GetCookieHeader(url);
            if (cookie.Length > 0)
            {
                request.Headers.Add("Cookie", cookie);
            }

            var invoker = url.Host == "server" ? _server : _web;
            var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);
            request.Dispose();
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var setCookie in setCookies)
                {
                    _cookies.SetCookies(url, setCookie);
                }
            }

            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location == null)
            {
                return response;
            }

            request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, response.Headers.Location));
            response.Dispose();
        }

        throw new InvalidOperationException("Too many redirects.");
    }

    public void Dispose()
    {
        _server.Dispose();
        _web.Dispose();
    }

    [GeneratedRegex("""<form[^>]*action=["']([^"']+)["']""")]
    private static partial Regex FormAction();

    [GeneratedRegex("""<input type=["']hidden["'] name=["']([^"']+)["'] value=["']([^"']*)["']""")]
    private static partial Regex HiddenInput();
}
