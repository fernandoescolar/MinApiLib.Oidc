namespace IdentityServer.UnitTests.Endpoints.NonceEndpointTests;

public class NonceEndpointTest
{
    private const string Category = "Nonce Endpoint";
    private IClientStore _clients = new InMemoryClientStore(TestClients.Get());

    [Fact]
    [Trait("Category", Category)]
    public async Task ok()
    {
        const string expectedNonce = "my super special nonce";
        const int expectedLifetime = 1;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var target = await CreateTarget(nonce: expectedNonce, lifetime: expectedLifetime);
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceResult>(result);

        var nonceResult = result as NonceResult;
        Assert.Equal(expectedNonce, nonceResult.Nonce);
        Assert.Equal(expectedLifetime, nonceResult.ExpiresIn);
    }


    [Fact]
    [Trait("Category", Category)]
    public async Task bad_method_fails()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var target = await CreateTarget();
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceErrorResult>(result);

        var nonceErrorResult = result as NonceErrorResult;
        Assert.Equal("invalid_request", nonceErrorResult.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task bad_content_type_fails()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/json";

        var target = await CreateTarget();
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceErrorResult>(result);

        var nonceErrorResult = result as NonceErrorResult;
        Assert.Equal("invalid_request", nonceErrorResult.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_not_found_fails()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var target = await CreateTarget(emptyClient: true);
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceErrorResult>(result);

        var nonceErrorResult = result as NonceErrorResult;
        Assert.Equal("invalid_client", nonceErrorResult.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task client_not_has_nonce_pattern_grant_type_fails()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var client = await _clients.FindEnabledClientByIdAsync("codeclient");

        var target = await CreateTarget(client: client);
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceErrorResult>(result);

        var nonceErrorResult = result as NonceErrorResult;
        Assert.Equal("invalid_grant", nonceErrorResult.Error);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task nonce_request_validation_error_fails()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";

        var target = await CreateTarget(nonceValidationError: true);
        var result = await target.ProcessAsync(httpContext);

        Assert.IsType<NonceErrorResult>(result);

        var nonceErrorResult = result as NonceErrorResult;
        Assert.Equal("invalid_validation", nonceErrorResult.Error);
    }

    private async Task<NonceEndpoint> CreateTarget(bool nonceValidationError = false, Client client = null, string nonce = "nonce", int lifetime = 100, bool emptyClient = false)
    {
        var validClient = emptyClient ? null : client ?? await _clients.FindEnabledClientByIdAsync("nonce_pattern");
        var clientValidationResult = new ClientSecretValidationResult
        {
            Client = validClient,
        };
        var nonceValidationResult = new NonceRequestValidationResult
        {
            IsError = nonceValidationError,
            Error = "invalid_validation",
            TokenValidationResult = new()
            {
                IsError = false,
                Client = validClient,
            }
        };
        var nonceResponse = new NonceResponse
        {
            Nonce = nonce,
            Lifetime = lifetime,
        };

        return new NonceEndpoint(
                    new StubClientSecretValidator(clientValidationResult),
                    new StubNonceRequestValidator(nonceValidationResult),
                    new StubNonceResponseGenerator(nonceResponse),
                    new FakeEventService(),
                    TestLogger.Create<NonceEndpoint>());
    }
}
