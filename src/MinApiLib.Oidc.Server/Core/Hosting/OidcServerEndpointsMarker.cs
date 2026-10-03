namespace MinApiLib.Oidc.Server.Core.Hosting;

/// <summary>
/// Records whether <c>MapOidcServer()</c> was called, so a host that forgets it fails with a clear error
/// instead of answering 404 on every protocol endpoint.
/// </summary>
[RegisterInIServiceCollection(ServiceLifetime = ServiceLifetime.Singleton)]
internal sealed class OidcServerEndpointsMarker
{
    public bool Mapped { get; set; }

    public void EnsureMapped()
    {
        if (!Mapped)
        {
            throw new InvalidOperationException(
                "The OidcServer endpoints are not mapped. Call app.MapOidcServer() after app.UseOidcServer().");
        }
    }
}
