namespace IdentityServer.UnitTests.Validation;

public class StrictRedirectUriValidatorValidation
{
    private const string Category = "Strict Redirect Uri Validator";

    [Theory]
    [Trait("Category", Category)]
    [InlineData("https://client.example/Callback?x=1", true)]
    [InlineData("HTTPS://CLIENT.EXAMPLE/Callback?x=1", true)]
    [InlineData("https://client.example/callback?x=1", false)]
    [InlineData("https://client.example/Callback?X=1", false)]
    [InlineData("https://client.example/Callback?x=1#f", false)]
    [InlineData("https://client.example:443/Callback?x=1", false)]
    [InlineData("https://other.example/Callback?x=1", false)]
    public async Task Only_scheme_and_host_are_case_insensitive(string requestedUri, bool expected)
    {
        var client = new Client
        {
            RedirectUris = { "https://client.example/Callback?x=1" },
            PostLogoutRedirectUris = { "https://client.example/Callback?x=1" }
        };
        var validator = new StrictRedirectUriValidator();

        Assert.Equal(expected, (await validator.IsRedirectUriValidAsync(requestedUri, client)));
        Assert.Equal(expected, (await validator.IsPostLogoutRedirectUriValidAsync(requestedUri, client)));
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData("com.Example.App:/oauth2redirect", true)]
    [InlineData("COM.EXAMPLE.APP:/oauth2redirect", true)]
    [InlineData("com.Example.App:/OAuth2redirect", false)]
    public async Task Custom_scheme_is_case_insensitive_but_path_is_exact(string requestedUri, bool expected)
    {
        var client = new Client { RedirectUris = { "com.Example.App:/oauth2redirect" } };

        Assert.Equal(expected, (await new StrictRedirectUriValidator().IsRedirectUriValidAsync(requestedUri, client)));
    }
}
