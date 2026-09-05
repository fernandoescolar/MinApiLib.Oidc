namespace IdentityServer.UnitTests.Services.Default;

public class DefaultCorsPolicyServiceTests
{
    private const string Category = "DefaultCorsPolicyService";

    private DefaultCorsPolicyService subject;

    public DefaultCorsPolicyServiceTests()
    {
        subject = new DefaultCorsPolicyService(TestLogger.Create<DefaultCorsPolicyService>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_null_param_ReturnsFalse()
    {
        var result = await subject.IsOriginAllowedAsync(null);
        Assert.False(result);

        result = await subject.IsOriginAllowedAsync(String.Empty);
        Assert.False(result);

        result = await subject.IsOriginAllowedAsync("    ");
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_OriginIsAllowed_ReturnsTrue()
    {
        subject.AllowedOrigins.Add("http://foo");
        var result = await subject.IsOriginAllowedAsync("http://foo");
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_OriginIsNotAllowed_ReturnsFalse()
    {
        subject.AllowedOrigins.Add("http://foo");
        var result = await subject.IsOriginAllowedAsync("http://bar");
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_OriginIsInAllowedList_ReturnsTrue()
    {
        subject.AllowedOrigins.Add("http://foo");
        subject.AllowedOrigins.Add("http://bar");
        subject.AllowedOrigins.Add("http://baz");
        var result = await subject.IsOriginAllowedAsync("http://bar");
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_OriginIsNotInAllowedList_ReturnsFalse()
    {
        subject.AllowedOrigins.Add("http://foo");
        subject.AllowedOrigins.Add("http://bar");
        subject.AllowedOrigins.Add("http://baz");
        var result = await subject.IsOriginAllowedAsync("http://quux");
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task IsOriginAllowed_AllowAllTrue_ReturnsTrue()
    {
        subject.AllowAll = true;
        var result = await subject.IsOriginAllowedAsync("http://foo");
        Assert.True(result);
    }
}
