namespace Samples.Server.Pages;

public sealed record HomePage() : GetHandler("/")
{
    protected override IResult Handle() => Html.Page("Sample OIDC server", """
        <p>MinApiLib.Oidc.Server running the sample configuration.</p>
        <ul>
          <li><a href="/.well-known/openid-configuration">Discovery document</a></li>
          <li><a href="/.well-known/openid-configuration/jwks">Signing keys (JWKS)</a></li>
        </ul>
        <p class="muted">Users: <code>alice</code> / <code>alice</code> and <code>bob</code> / <code>bob</code>.</p>
        """);
}
