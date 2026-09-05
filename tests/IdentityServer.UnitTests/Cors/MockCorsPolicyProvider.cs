namespace IdentityServer.UnitTests.Cors;

public class MockCorsPolicyProvider : ICorsPolicyProvider
{
    public bool WasCalled { get; set; }
    public CorsPolicy Response { get; set; }

    public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
    {
        WasCalled = true;
        return Task.FromResult(Response);
    }
}
