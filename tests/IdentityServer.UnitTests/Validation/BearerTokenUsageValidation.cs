namespace IdentityServer.UnitTests.Validation;

public class BearerTokenUsageValidation
{
    private const string Category = "BearerTokenUsageValidator Tests";

    [Fact]
    [Trait("Category", Category)]
    public async Task No_Header_no_Body_Get()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task No_Header_no_Body_Post()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Non_Bearer_Scheme_Header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.Append("Authorization", new string[] { "Foo Bar" });

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Empty_Bearer_Scheme_Header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.Append("Authorization", new string[] { "Bearer" });

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Whitespaces_Bearer_Scheme_Header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.Append("Authorization", new string[] { "Bearer           " });

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Bearer_Scheme_Header()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "GET";
        ctx.Request.Headers.Append("Authorization", new string[] { "Bearer token" });

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.True(result.TokenFound);
        Assert.Equal("token", result.Token);
        Assert.Equal(BearerTokenUsageType.AuthorizationHeader, result.UsageType);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Valid_Body_Post()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var body = "access_token=token";
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.True(result.TokenFound);
        Assert.Equal("token", result.Token);
        Assert.Equal(BearerTokenUsageType.PostBody, result.UsageType);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Body_Post_empty_Token()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var body = "access_token=";
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Body_Post_Whitespace_Token()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var body = "access_token=                ";
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task Body_Post_no_Token()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var body = "foo=bar";
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var validator = new BearerTokenUsageValidator(TestLogger.Create<BearerTokenUsageValidator>());
        var result = await validator.ValidateAsync(ctx);

        Assert.False(result.TokenFound);
    }
}
