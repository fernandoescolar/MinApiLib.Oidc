namespace IdentityServer.UnitTests.Validation.Secrets;

public class ClientSecretValidation
{
    private const string Category = "Secrets - Client Secret Validator";

    [Fact]
    [Trait("Category", Category)]
    public async Task confidential_client_with_correct_secret_should_be_able_to_request_token()
    {
        var validator = Factory.CreateClientSecretValidator();

        var context = new DefaultHttpContext();
        var body = "client_id=roclient&client_secret=secret";

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/x-www-form-urlencoded";

        var result = await validator.ValidateAsync(context);

        Assert.False(result.IsError);
        Assert.Equal("roclient", result.Client.ClientId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task confidential_client_with_incorrect_secret_should_not_be_able_to_request_token()
    {
        var validator = Factory.CreateClientSecretValidator();

        var context = new DefaultHttpContext();
        var body = "client_id=roclient&client_secret=invalid";

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/x-www-form-urlencoded";

        var result = await validator.ValidateAsync(context);

        Assert.True(result.IsError);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task public_client_without_secret_should_be_able_to_request_token()
    {
        var validator = Factory.CreateClientSecretValidator();

        var context = new DefaultHttpContext();
        var body = "client_id=roclient.public";

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/x-www-form-urlencoded";

        var result = await validator.ValidateAsync(context);

        Assert.False(result.IsError);
        Assert.Equal("roclient.public", result.Client.ClientId);
        Assert.False(result.Client.RequireClientSecret);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task code_client_without_secret_should_be_able_to_authenticate()
    {
        var validator = Factory.CreateClientSecretValidator();

        var context = new DefaultHttpContext();
        var body = "client_id=client.code_only";

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/x-www-form-urlencoded";

        var result = await validator.ValidateAsync(context);

        Assert.False(result.IsError);
        Assert.Equal("client.code_only", result.Client.ClientId);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task code_and_client_creds_client_without_secret_should_not_be_able_to_authenticate()
    {
        var validator = Factory.CreateClientSecretValidator();

        var context = new DefaultHttpContext();
        var body = "client_id=code_and_client_creds";

        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/x-www-form-urlencoded";

        var result = await validator.ValidateAsync(context);

        Assert.True(result.IsError);
    }
}
