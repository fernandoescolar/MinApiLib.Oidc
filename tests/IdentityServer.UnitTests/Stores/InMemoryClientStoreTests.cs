namespace IdentityServer.UnitTests.Stores;

public class InMemoryClientStoreTests
{
    [Fact]
    public void InMemoryClient_should_throw_if_contain_duplicate_client_ids()
    {
        List<Client> clients = new List<Client>
            {
                new Client { ClientId = "1"},
                new Client { ClientId = "1"},
                new Client { ClientId = "3"}
            };

        Action act = () => new InMemoryClientStore(clients);
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void InMemoryClient_should_not_throw_if_does_not_contain_duplicate_client_ids()
    {
        List<Client> clients = new List<Client>
            {
                new Client { ClientId = "1"},
                new Client { ClientId = "2"},
                new Client { ClientId = "3"}
            };

        new InMemoryClientStore(clients);
    }
}
