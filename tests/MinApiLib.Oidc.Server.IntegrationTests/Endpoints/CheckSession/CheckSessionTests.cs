namespace MinApiLib.Oidc.Server.IntegrationTests.Endpoints.CheckSession;

public class CheckSessionTests : IDisposable
{
    private const string Category = "Check session endpoint";

    private OidcServerPipeline _mockPipeline = new OidcServerPipeline();

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
        var response = await _mockPipeline.BackChannelClient.GetAsync(OidcServerPipeline.CheckSessionEndpoint, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
