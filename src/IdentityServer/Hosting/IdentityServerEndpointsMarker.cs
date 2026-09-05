namespace IdentityServer.Hosting;

/// <summary>
/// Records whether <c>MapIdentityServer()</c> was called, so a host that forgets it fails with a clear error
/// instead of answering 404 on every protocol endpoint.
/// </summary>
internal sealed class IdentityServerEndpointsMarker
{
    public bool Mapped { get; set; }

    public void EnsureMapped()
    {
        if (!Mapped)
        {
            throw new InvalidOperationException(
                "The IdentityServer endpoints are not mapped. Call app.MapIdentityServer() after app.UseIdentityServer().");
        }
    }
}
