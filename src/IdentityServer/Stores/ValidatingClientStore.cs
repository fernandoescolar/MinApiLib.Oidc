namespace IdentityServer.Stores;

/// <summary>
/// Client store decorator for running runtime configuration validation checks
/// </summary>
public class ValidatingClientStore<T> : IClientStore
    where T : IClientStore
{
    private readonly IClientStore _inner;
    private readonly IClientConfigurationValidator _validator;
    private readonly IEventService _events;
    private readonly ILogger<ValidatingClientStore<T>> _logger;
    private readonly string _validatorType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatingClientStore{T}" /> class.
    /// </summary>
    /// <param name="inner">The inner.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="events">The events.</param>
    /// <param name="logger">The logger.</param>
    public ValidatingClientStore(T inner, IClientConfigurationValidator validator, IEventService events, ILogger<ValidatingClientStore<T>> logger)
    {
        _inner = inner;
        _validator = validator;
        _events = events;
        _logger = logger;

        // the runtime type of an instance always has a full name
        _validatorType = validator.GetType().FullName!;
    }

    /// <summary>
    /// Finds a client by id (and runs the validation logic)
    /// </summary>
    /// <param name="clientId">The client id</param>
    /// <returns>
    /// The client or an InvalidOperationException
    /// </returns>
    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        var client = await _inner.FindClientByIdAsync(clientId);

        if (client != null)
        {
            SecretLog.CallingClientConfigurationValidator(_logger, _validatorType);

            var context = new ClientConfigurationValidationContext(client);
            await _validator.ValidateAsync(context);

            if (context.IsValid)
            {
                SecretLog.ClientConfigurationValidationSucceeded(_logger, client.ClientId);
                return client;
            }

            SecretLog.InvalidClientConfiguration(_logger, client.ClientId, context.ErrorMessage);
            await _events.RaiseAsync(new InvalidClientConfigurationEvent(client, context.ErrorMessage));

            return null;
        }

        return null;
    }
}
