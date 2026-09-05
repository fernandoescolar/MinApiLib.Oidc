using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IdentityServer.UnitTests.Configuration;

public class IdentityServerOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(Action<IdentityServerOptions> configure)
    {
        var options = new IdentityServerOptions();
        configure(options);
        return new IdentityServerOptionsValidator().Validate(Options.DefaultName, options);
    }

    [Fact]
    public void Default_options_are_valid()
    {
        Assert.True(Validate(_ => { }).Succeeded);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://issuer")]
    [InlineData("https://issuer?x=1")]
    [InlineData("https://issuer#f")]
    public void Invalid_issuer_is_rejected(string issuer)
    {
        Assert.True(Validate(o => o.IssuerUri = issuer).Failed);
    }

    [Fact]
    public void Inconsistent_values_are_all_reported()
    {
        var result = Validate(o =>
        {
            o.Authentication.CookieLifetime = TimeSpan.Zero;
            o.InputLengthRestrictions.ClientId = 0;
            o.PushedAuthorization.Lifetime = -1;
            o.DPoP.SupportedSigningAlgorithms = ["HS256"];
            o.KeyManagement.Enabled = true;
            o.KeyManagement.PropagationTime = TimeSpan.FromDays(100);
        });

        Assert.True(result.Failed);
        Assert.Equal(5, result.Failures.Count());
        Assert.Contains(result.Failures, f => f.Contains("InputLengthRestrictions.ClientId"));
    }

    [Fact]
    public void Key_cache_must_be_shorter_than_propagation_time()
    {
        Assert.True(Validate(o =>
        {
            o.KeyManagement.Enabled = true;
            o.KeyManagement.KeyCacheDuration = TimeSpan.FromDays(20);
        }).Failed);
    }

    [Fact]
    public async Task Host_does_not_start_with_invalid_options()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddIdentityServer(o => o.PushedAuthorization.Lifetime = 0);
        using var host = builder.Build();

        var act = () => host.StartAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OptionsValidationException>(act);
    }
}
