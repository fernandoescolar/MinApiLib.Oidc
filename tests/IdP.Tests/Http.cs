namespace IdP.Tests;

/// <summary>HTTP calls with the test's cancellation token and relative paths.</summary>
public static class Http
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> Get(this HttpClient client, string path) => client.GetAsync(new Uri(path, UriKind.Relative), Ct);

    public static Task<JsonElement> GetJson(this HttpClient client, string path) => client.GetFromJsonAsync<JsonElement>(new Uri(path, UriKind.Relative), Ct);

    public static Task<HttpResponseMessage> PostForm(this HttpClient client, string path, Dictionary<string, string> form)
        => client.PostAsync(new Uri(path, UriKind.Relative), new FormUrlEncodedContent(form), Ct);

    public static Task<HttpResponseMessage> PostJson(this HttpClient client, string path, object body) => client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, Ct);

    public static Task<HttpResponseMessage> PutJson(this HttpClient client, string path, object body) => client.PutAsJsonAsync(new Uri(path, UriKind.Relative), body, Ct);

    public static Task<HttpResponseMessage> Delete(this HttpClient client, string path) => client.DeleteAsync(new Uri(path, UriKind.Relative), Ct);

    public static Task<JsonElement> Json(this HttpResponseMessage response) => response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    public static Task<string> Text(this HttpResponseMessage response) => response.Content.ReadAsStringAsync(Ct);
}
