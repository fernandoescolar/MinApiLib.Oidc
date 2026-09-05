namespace IdentityServer.IntegrationTests.Endpoints.CheckSession;

public class CheckSessionTests : IDisposable
{
    private const string Category = "Check session endpoint";

    private IdentityServerPipeline _mockPipeline = new IdentityServerPipeline();

    public void Dispose()
    {
        _mockPipeline.Dispose();
        GC.SuppressFinalize(this);
    }


    public CheckSessionTests()
    {
        _mockPipeline.Initialize();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task get_request_should_not_return_404()
    {
        var response = await _mockPipeline.BackChannelClient.GetAsync(IdentityServerPipeline.CheckSessionEndpoint, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
