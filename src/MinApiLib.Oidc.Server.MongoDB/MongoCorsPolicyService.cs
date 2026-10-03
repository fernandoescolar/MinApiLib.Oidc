namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// CORS policy service that allows the origins listed in <see cref="Client.AllowedCorsOrigins"/> of any client in
/// the <c>clients</c> collection.
/// </summary>
public class MongoCorsPolicyService : ICorsPolicyService
{
    private readonly OidcServerMongoContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCorsPolicyService"/> class.
    /// </summary>
    public MongoCorsPolicyService(OidcServerMongoContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> IsOriginAllowedAsync(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        var filter = Builders<Client>.Filter.AnyEq(c => c.AllowedCorsOrigins, origin);
        var options = new CountOptions { Collation = OidcServerMongoContext.CaseInsensitive, Limit = 1 };
        return await _context.Clients.CountDocumentsAsync(filter, options) > 0;
    }
}
