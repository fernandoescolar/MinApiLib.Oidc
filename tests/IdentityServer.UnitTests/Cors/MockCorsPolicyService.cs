namespace IdentityServer.UnitTests.Cors;

public class MockCorsPolicyService : ICorsPolicyService
{
    public bool WasCalled { get; set; }
    public bool Response { get; set; }

    public Task<bool> IsOriginAllowedAsync(string origin)
    {
        WasCalled = true;
        return Task.FromResult(Response);
    }
}
