using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace MinApiLib.OidcServer.IntegrationTests.Endpoints.MutualTls;

/// <summary>
/// mTLS client authentication through the /connect/mtls path, with the middleware router and with endpoint routing.
/// The certificate authentication scheme is a test double that accepts any certificate present on the connection.
/// </summary>
public class MutualTlsEndpointTests
{
    private const string Category = "Mutual TLS";

    private sealed class TestCertificateHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestCertificateHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Connection.ClientCertificate == null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "certificate")], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private static OidcServerPipeline CreatePipeline()
    {
        var pipeline = new OidcServerPipeline();
        pipeline.Clients.Add(new Client
        {
            ClientId = "mtls.client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api1" },
            ClientSecrets =
            {
                new Secret(TestCert.Load().Thumbprint) { Type = OidcServerConstants.SecretTypes.X509CertificateThumbprint }
            }
        });
        pipeline.ApiScopes.Add(new ApiScope("api1"));
        pipeline.OnPostConfigureServices += services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestCertificateHandler>("Certificate", _ => { });
            services.AddTransient<ISecretParser, MutualTlsSecretParser>();
            services.AddTransient<ISecretValidator, X509ThumbprintSecretValidator>();
        };
        pipeline.Initialize();
        pipeline.Options.MutualTls.Enabled = true;
        return pipeline;
    }

    private static HttpClient CreateClient(OidcServerPipeline pipeline, bool withCertificate)
        => new(pipeline.Server.CreateHandler(context =>
        {
            if (withCertificate)
            {
                context.Connection.ClientCertificate = TestCert.Load();
            }
        }));

    private static Task<HttpResponseMessage> RequestTokenAsync(HttpClient client, string path)
        => client.PostAsync("https://server" + path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", "mtls.client" },
            { "scope", "api1" }
        }), TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Category", Category)]
    public async Task Certificate_on_mtls_endpoint_authenticates_the_client_and_binds_the_token()
    {
        using var pipeline = CreatePipeline();
        using var client = CreateClient(pipeline, withCertificate: true);

        var response = await RequestTokenAsync(client, "/connect/mtls/token");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).AsObject();
        var accessToken = new JwtSecurityTokenHandler().ReadJwtToken(json["access_token"].ToString());
        Assert.Contains(accessToken.Claims, c => c.Type == "cnf");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Certificate_on_the_regular_token_endpoint_is_not_trusted()
    {
        using var pipeline = CreatePipeline();
        using var client = CreateClient(pipeline, withCertificate: true);

        var response = await RequestTokenAsync(client, "/connect/token");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(OidcConstants.TokenErrors.InvalidClient, (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Mtls_endpoint_without_certificate_is_forbidden()
    {
        using var pipeline = CreatePipeline();
        using var client = CreateClient(pipeline, withCertificate: false);

        var response = await RequestTokenAsync(client, "/connect/mtls/token");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
