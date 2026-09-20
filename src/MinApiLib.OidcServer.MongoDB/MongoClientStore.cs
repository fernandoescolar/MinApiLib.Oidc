namespace MinApiLib.OidcServer.MongoDB;

/// <summary>
/// Client store backed by the <c>clients</c> collection.
/// </summary>
public class MongoClientStore : IClientStore
{
    private readonly OidcServerMongoContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoClientStore"/> class.
    /// </summary>
    public MongoClientStore(OidcServerMongoContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<Client?> FindClientByIdAsync(string clientId)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        return _context.Clients.Find(c => c.ClientId == clientId).FirstOrDefaultAsync()!;
    }
}
