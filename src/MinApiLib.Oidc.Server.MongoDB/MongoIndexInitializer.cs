namespace MinApiLib.Oidc.Server.MongoDB;

/// <summary>
/// Creates the indexes of the MongoDB stores when the host starts.
/// </summary>
internal sealed partial class MongoIndexInitializer : IHostedService
{
    private readonly OidcServerMongoContext _context;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(OidcServerMongoContext context, ILogger<MongoIndexInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _context.CreateIndexesAsync(cancellationToken);
        IndexesCreated(_logger, _context.Database.DatabaseNamespace.DatabaseName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(0x7000, LogLevel.Debug, "Created the OidcServer indexes in the MongoDB database {database}")]
    private static partial void IndexesCreated(ILogger logger, string database);
}
