namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

public class MessageHandlerWrapper : DelegatingHandler
{
    public HttpResponseMessage Response { get; set; }

    public MessageHandlerWrapper(HttpMessageHandler handler)
        : base(handler)
    {
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Response = await base.SendAsync(request, cancellationToken);
        return Response;
    }
}
