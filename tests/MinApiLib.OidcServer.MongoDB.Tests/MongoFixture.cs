[assembly: AssemblyFixture(typeof(MinApiLib.OidcServer.MongoDB.Tests.MongoFixture))]

namespace MinApiLib.OidcServer.MongoDB.Tests;

/// <summary>
/// One MongoDB container for the test run. Each test uses its own database.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0").Build();

    public string ConnectionString => _container.GetConnectionString();

    public IMongoClient Client { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Client = new MongoClient(ConnectionString);
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public OidcServerMongoContext CreateContext(string prefix = "")
        => new(Client.GetDatabase("test_" + Guid.NewGuid().ToString("N")), prefix);
}
