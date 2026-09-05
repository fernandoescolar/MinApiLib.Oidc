namespace IdentityServer.UnitTests.Validation;

public class GrantTypesValidation
{
    private const string Category = "Grant Types Validation";

    [Fact]
    [Trait("Category", Category)]
    public void empty_should_be_allowed()
    {
        var client = new Client();
        client.AllowedGrantTypes = new List<string>();
    }

    [Fact]
    [Trait("Category", Category)]
    public void custom_should_be_allowed()
    {
        var client = new Client();
        client.AllowedGrantTypes = new[] { "custom" };
    }

    [Fact]
    [Trait("Category", Category)]
    public void custom_should_be_allowed_raw()
    {
        var client = new Client();
        client.AllowedGrantTypes = new[] { "custom" };
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(GrantType.AuthorizationCode, GrantType.Hybrid)]
    public void forbidden_grant_type_combinations_should_throw(string type1, string type2)
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = new[] { type1, type2 };

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Theory]
    [Trait("Category", Category)]
    [InlineData(GrantType.AuthorizationCode, GrantType.Hybrid)]
    public void custom_and_forbidden_grant_type_combinations_should_throw(string type1, string type2)
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = new[] { "custom1", type2, "custom2", type1 };

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void duplicate_values_should_throw()
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = new[] { "custom1", "custom2", "custom1" };

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void null_grant_type_list_should_throw_single()
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = null;

        Assert.ThrowsAny<ArgumentNullException>(act);
    }

    [Fact]
    public void grant_type_with_space_should_throw_single()
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = new[] { "custo m2" };

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void grant_type_with_space_should_throw_multiple()
    {
        var client = new Client();

        Action act = () => client.AllowedGrantTypes = new[] { "custom1", "custo m2", "custom1" };

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void adding_invalid_value_to_collection_should_throw()
    {
        var client = new Client()
        {
            AllowedGrantTypes = { "authorization_code" }
        };

        Action act = () => client.AllowedGrantTypes.Add("hybrid");

        Assert.ThrowsAny<InvalidOperationException>(act);
    }

    [Fact]
    public void adding_valid_value_to_collection_should_succeed()
    {
        var client = new Client()
        {
            AllowedGrantTypes = { "custom" }
        };

        client.AllowedGrantTypes.Add("custom2");

        Assert.Equal(2, client.AllowedGrantTypes.Count);
    }
}
