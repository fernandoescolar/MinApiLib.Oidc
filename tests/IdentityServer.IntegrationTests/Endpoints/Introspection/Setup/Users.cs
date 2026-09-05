namespace IdentityServer.IntegrationTests.Endpoints.Introspection.Setup;

public static class Users
{
    public static List<TestUser> Get()
    {
        return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "bob",
                    Password = "bob"
                }
            };
    }
}
