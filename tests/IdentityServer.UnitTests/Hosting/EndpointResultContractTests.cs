namespace IdentityServer.UnitTests.Hosting;

public class EndpointResultContractTests
{
    [Fact]
    public void Endpoint_results_are_aspnetcore_results()
    {
        Assert.True(typeof(IResult).IsAssignableFrom(typeof(IEndpointResult)));
    }

    [Fact]
    public void Public_endpoint_names_match_the_registered_endpoint_names()
    {
        Assert.Equal("Token", IdentityServerConstants.EndpointNames.Token);
        Assert.Equal("Discovery", IdentityServerConstants.EndpointNames.Discovery);
    }
}
