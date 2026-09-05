namespace IdentityServer.UnitTests.Services.Default;

public class NumericUserCodeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_should_return_expected_code()
    {
        var sut = new NumericUserCodeGenerator();

        var userCode = await sut.GenerateAsync();
        var userCodeInt = int.Parse(userCode);

        Assert.True(userCodeInt >= 100000000);
        Assert.True(userCodeInt <= 999999999);
    }
}
