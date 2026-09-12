using MinApiLib.OidcServer.Core.ClientAuthentication;
using MinApiLib.OidcServer.Core.Clients;
using MinApiLib.OidcServer.Core.Events;
using MinApiLib.OidcServer.Core.TokenServices;
using MinApiLib.OidcServer.Features.DeviceFlow;
using MinApiLib.OidcServer.Features.Interaction;
using MinApiLib.OidcServer.Features.Introspection;
using MinApiLib.OidcServer.Features.Nonce;
using MinApiLib.OidcServer.Features.Revocation;
using MinApiLib.OidcServer.Features.Session;
using MinApiLib.OidcServer.Core.Cors;
using MinApiLib.OidcServer.Core.DPoP;
using MinApiLib.OidcServer.Core.Hosting;
using MinApiLib.OidcServer.Features.Authorize;
using MinApiLib.OidcServer.Features.Discovery;
using MinApiLib.OidcServer.Features.Tokens;
using MinApiLib.OidcServer.Features.UserInfo;
using Microsoft.AspNetCore.Routing;

namespace MinApiLib.OidcServer.IntegrationTests.Hosting;

/// <summary>
/// The protocol endpoints are MinApiLib endpoint records and the default services are registered with MinApiLib's
/// dependency injection attribute.
/// </summary>
public class MinApiLibHostingTests : IDisposable
{
    private const string Category = "MinApiLib hosting";

    private readonly OidcServerPipeline _pipeline = new();

    public void Dispose()
    {
        _pipeline.Dispose();
        GC.SuppressFinalize(this);
    }

    private List<OidcServerEndpointMetadata> MappedEndpoints()
        => _pipeline.Server.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => e.Metadata.GetMetadata<OidcServerEndpointMetadata>())
            .Where(m => m != null)
            .ToList();

    [Fact]
    [Trait("Category", Category)]
    public void Every_protocol_route_is_a_MinApiLib_endpoint_record()
    {
        _pipeline.Initialize();

        var endpoints = MappedEndpoints();

        // 16 routes (authorize and end session have one per method), and the connect/mtls alias of the 14 under connect/
        Assert.Equal(30, endpoints.Count);
        Assert.Equal(14, endpoints.Count(e => e.IsMutualTlsAlias));
        Assert.All(endpoints, e =>
        {
            Assert.True(typeof(MinApiLib.Endpoints.IEndpoint).IsAssignableFrom(e.EndpointType), e.EndpointType.Name);
            Assert.True(e.EndpointType.IsSealed);
        });
        Assert.Equal(30, endpoints.Select(e => e.EndpointType).Distinct().Count());
    }

    [Theory]
    [InlineData("GET", OidcServerPipeline.TokenEndpoint, "POST")]
    [InlineData("GET", OidcServerPipeline.IntrospectionEndpoint, "POST")]
    [InlineData("GET", OidcServerPipeline.RevocationEndpoint, "POST")]
    [InlineData("PUT", OidcServerPipeline.AuthorizeEndpoint, "GET,POST")]
    [InlineData("DELETE", OidcServerPipeline.UserInfoEndpoint, "GET,POST")]
    [InlineData("POST", OidcServerPipeline.DiscoveryEndpoint, "GET")]
    [InlineData("POST", OidcServerPipeline.CheckSessionEndpoint, "GET")]
    [Trait("Category", Category)]
    public async Task Each_endpoint_is_mapped_for_its_methods_only(string method, string url, string allowed)
    {
        _pipeline.Initialize();

        using var message = new HttpRequestMessage(new HttpMethod(method), url);
        var response = await _pipeline.BackChannelClient.SendAsync(message, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(allowed.Split(','), response.Content.Headers.Allow.Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Authorize_post_requires_a_form()
    {
        _pipeline.Initialize();

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _pipeline.BackChannelClient.PostAsync(OidcServerPipeline.AuthorizeEndpoint, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Library_does_not_reference_IdentityServer()
    {
        var references = typeof(OidcServerOptions).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

        Assert.DoesNotContain(references, r => r.StartsWith("IdentityServer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, r => r.StartsWith("Duende", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("MinApiLib.Endpoints", references);
        Assert.Contains("MinApiLib.DependencyInjection", references);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Default_services_are_registered_from_their_attribute()
    {
        _pipeline.Initialize();
        using var scope = _pipeline.Server.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.IsType<DefaultTokenService>(services.GetRequiredService<ITokenService>());
        Assert.IsType<TokenResponseGenerator>(services.GetRequiredService<ITokenResponseGenerator>());
        Assert.IsType<StrictRedirectUriValidator>(services.GetRequiredService<IRedirectUriValidator>());
        Assert.Equal(2, services.GetServices<ISecretParser>().Count());
        Assert.Same(services.GetRequiredService<BackChannelLogoutQueue>(), _pipeline.Server.Services.GetRequiredService<BackChannelLogoutQueue>());
    }

    [Fact]
    [Trait("Category", Category)]
    public void Services_registered_before_or_after_AddOidcServer_replace_the_default()
    {
        var before = new StubTokenService();
        var after = new StubProfileService();
        _pipeline.OnPreConfigureServices += services => services.AddSingleton<ITokenService>(before);
        _pipeline.OnPostConfigureServices += services => services.AddSingleton<IProfileService>(after);
        _pipeline.Initialize();
        using var scope = _pipeline.Server.Services.CreateScope();

        Assert.Same(before, scope.ServiceProvider.GetRequiredService<ITokenService>());
        Assert.Same(after, scope.ServiceProvider.GetRequiredService<IProfileService>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Unhandled_exception_raises_an_event()
    {
        var sink = new CapturingEventSink();
        _pipeline.OnPostConfigureServices += services =>
        {
            services.AddSingleton<IEventSink>(sink);
            services.AddTransient<IDiscoveryResponseGenerator, ThrowingDiscoveryResponseGenerator>();
        };
        _pipeline.Initialize();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _pipeline.BackChannelClient.GetAsync(OidcServerPipeline.DiscoveryEndpoint, TestContext.Current.CancellationToken));

        Assert.Contains(sink.Events, e => e is UnhandledExceptionEvent);
    }

    private sealed class CapturingEventSink : IEventSink
    {
        public List<Event> Events { get; } = [];

        public Task PersistAsync(Event evt)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDiscoveryResponseGenerator : IDiscoveryResponseGenerator
    {
        public Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
            => throw new InvalidOperationException("boom");

        public Task<IEnumerable<MinApiLib.OidcServer.Features.Discovery.JsonWebKey>> CreateJwkDocumentAsync()
            => throw new InvalidOperationException("boom");
    }

    private sealed class StubTokenService : ITokenService
    {
        public Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request) => throw new NotSupportedException();

        public Task<Token> CreateAccessTokenAsync(TokenCreationRequest request) => throw new NotSupportedException();

        public Task<string> CreateSecurityTokenAsync(Token token) => throw new NotSupportedException();
    }

    private sealed class StubProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context) => Task.CompletedTask;

        public Task IsActiveAsync(IsActiveContext context) => Task.CompletedTask;
    }
}
