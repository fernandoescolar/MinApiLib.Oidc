namespace MinApiLib.OidcServer.Core.Cors;

/// <summary>
/// CORS policy service that configures the allowed origins from a list of clients' redirect URLs.
/// </summary>
public class InMemoryCorsPolicyService : ICorsPolicyService
{
    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger;
    /// <summary>
    /// Clients applications list
    /// </summary>
    protected readonly IEnumerable<Client> Clients;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCorsPolicyService"/> class.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="clients">The clients.</param>
    public InMemoryCorsPolicyService(ILogger<InMemoryCorsPolicyService> logger, IEnumerable<Client> clients)
    {
        Logger = logger;
        Clients = clients ?? Enumerable.Empty<Client>();
    }

    /// <summary>
    /// Determines whether origin is allowed.
    /// </summary>
    /// <param name="origin">The origin.</param>
    /// <returns></returns>
    public virtual Task<bool> IsOriginAllowedAsync(string origin)
    {
        var query =
            from client in Clients
            from url in client.AllowedCorsOrigins
            select url.GetOrigin();

        var result = query.Contains(origin, StringComparer.OrdinalIgnoreCase);

        if (result)
        {
            ServicesLog.ClientListOriginAllowed(Logger, origin);
        }
        else
        {
            ServicesLog.ClientListOriginNotAllowed(Logger, origin);
        }

        return Task.FromResult(result);
    }
}
