namespace Samples.WebClient.DPoP;

/// <summary>
/// The back-channel handler of the DPoP OpenID Connect scheme. When the handler redeems the authorization code, it
/// adds a proof of the session key, created in <c>OnAuthorizationCodeReceived</c>, to the token request.
/// </summary>
public sealed class DPoPTokenRequestHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public static HttpMessageHandler Create(IHttpContextAccessor accessor)
        => new DPoPTokenRequestHandler(accessor) { InnerHandler = new HttpClientHandler() };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post &&
            request.RequestUri != null &&
            accessor.HttpContext?.Items[DPoPKey.PropertyName] is DPoPKey key)
        {
            request.Headers.Add("DPoP", DPoPProof.Create(key, "POST", request.RequestUri));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
