namespace IdentityServer.UnitTests.Infrastructure;

public class ObjectSerializerTests
{
    public ObjectSerializerTests()
    {
    }

    [Fact]
    public void Can_be_deserialize_message()
    {
        Action a = () => IdentityServer.ObjectSerializer.FromString<Message<ErrorMessage>>("{\"created\":0, \"data\": {\"error\": \"error\"}}");
        Assert.Null(Record.Exception(a));
    }
}
