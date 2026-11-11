using MinApiLib.Oidc.Server.Core.ClientAuthentication;
using MinApiLib.Oidc.Server.Core.Clients;
using MinApiLib.Oidc.Server.Core.Events;
using MinApiLib.Oidc.Server.Core.TokenServices;
using MinApiLib.Oidc.Server.Features.DeviceFlow;
using MinApiLib.Oidc.Server.Features.Interaction;
using MinApiLib.Oidc.Server.Features.Introspection;
using MinApiLib.Oidc.Server.Features.Nonce;
using MinApiLib.Oidc.Server.Features.Revocation;
using MinApiLib.Oidc.Server.Features.Session;
using MinApiLib.Oidc.Server.Core.Cors;
using MinApiLib.Oidc.Server.Core.DPoP;
using MinApiLib.Oidc.Server.Core.Hosting;
using MinApiLib.Oidc.Server.Features.Authorize;
using MinApiLib.Oidc.Server.Features.Discovery;
using MinApiLib.Oidc.Server.Features.Tokens;
using MinApiLib.Oidc.Server.Features.UserInfo;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace MinApiLib.Oidc.Server.IntegrationTests.Hosting;

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
    public void Return_url_parsers_registered_before_AddOidcServer_are_added_next_to_the_default()
    {
        var parser = new StubReturnUrlParser();
        _pipeline.OnPreConfigureServices += services => services.AddSingleton<IReturnUrlParser>(parser);
        _pipeline.Initialize();
        using var scope = _pipeline.Server.Services.CreateScope();

        var parsers = scope.ServiceProvider.GetServices<IReturnUrlParser>().ToList();
        Assert.Contains(parser, parsers);
        Assert.Contains(parsers, p => p is OidcReturnUrlParser);
    }

    [Fact]
    [Trait("Category", Category)]
    public void Login_and_logout_urls_default_to_the_server_routes_and_configure_the_session_cookie()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOidcServer();
        using var provider = services.BuildServiceProvider();

        var interaction = provider.GetRequiredService<OidcServerOptions>().UserInteraction;
        Assert.Equal("/account/login", interaction.LoginUrl);
        Assert.Equal("returnUrl", interaction.LoginReturnUrlParameter);
        Assert.Equal("/account/logout", interaction.LogoutUrl);

        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(OidcServerConstants.DefaultCookieAuthenticationScheme);
        Assert.Equal("/account/login", cookie.LoginPath);
        Assert.Equal("returnUrl", cookie.ReturnUrlParameter);
        Assert.Equal("/account/logout", cookie.LogoutPath);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void UserCodeMaxFailedAttempts_can_be_zero_but_not_negative(int value, bool valid)
    {
        var options = new OidcServerOptions();
        options.DeviceFlow.UserCodeMaxFailedAttempts = value;

        var result = new OidcServerOptionsValidator().Validate(null, options);

        Assert.Equal(valid, result.Succeeded);
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

        public Task<IEnumerable<MinApiLib.Oidc.Server.Features.Discovery.JsonWebKey>> CreateJwkDocumentAsync()
            => throw new InvalidOperationException("boom");
    }

    private sealed class StubTokenService : ITokenService
    {
        public Task<Token> CreateIdentityTokenAsync(TokenCreationRequest request) => throw new NotSupportedException();

        public Task<Token> CreateAccessTokenAsync(TokenCreationRequest request) => throw new NotSupportedException();

        public Task<string> CreateSecurityTokenAsync(Token token) => throw new NotSupportedException();
    }

    private sealed class StubReturnUrlParser : IReturnUrlParser
    {
        public Task<AuthorizationRequest> ParseAsync(string returnUrl) => Task.FromResult<AuthorizationRequest>(null);

        public bool IsValidReturnUrl(string returnUrl) => false;
    }

    private sealed class StubProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context) => Task.CompletedTask;

        public Task IsActiveAsync(IsActiveContext context) => Task.CompletedTask;
    }
}
