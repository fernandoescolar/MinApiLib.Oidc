namespace IdentityServer.Validation;

/// <summary>
/// Validates a client secret using the registered secret validators and parsers
/// </summary>
public class ClientSecretValidator : IClientSecretValidator
{
    private readonly ILogger _logger;
    private readonly IClientStore _clients;
    private readonly IEventService _events;
    private readonly ISecretsListValidator _validator;
    private readonly ISecretsListParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientSecretValidator"/> class.
    /// </summary>
    /// <param name="clients">The clients.</param>
    /// <param name="parser">The parser.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="events">The events.</param>
    /// <param name="logger">The logger.</param>
    public ClientSecretValidator(IClientStore clients, ISecretsListParser parser, ISecretsListValidator validator, IEventService events, ILogger<ClientSecretValidator> logger)
    {
        _clients = clients;
        _parser = parser;
        _validator = validator;
        _events = events;
        _logger = logger;
    }

    /// <summary>
    /// Validates the current request.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public async Task<ClientSecretValidationResult> ValidateAsync(HttpContext context)
    {
        SecretLog.StartClientValidation(_logger);

        var fail = new ClientSecretValidationResult
        {
            IsError = true
        };

        var parsedSecret = await _parser.ParseAsync(context);
        if (parsedSecret == null)
        {
            await RaiseFailureEventAsync("unknown", "No client id found");

            SecretLog.NoClientIdentifierFound(_logger);
            return fail;
        }

        var client = await _clients.FindEnabledClientByIdAsync(parsedSecret.Id);
        if (client == null)
        {
            await RaiseFailureEventAsync(parsedSecret.Id, "Unknown client");

            SecretLog.ClientNotFound(_logger, parsedSecret.Id);
            return fail;
        }

        SecretValidationResult? secretValidationResult = null;
        if (!client.RequireClientSecret)
        {
            SecretLog.PublicClientSkippingSecretValidation(_logger);
        }
        else
        {
            secretValidationResult = await _validator.ValidateAsync(client.ClientSecrets, parsedSecret);
            if (secretValidationResult.Success == false)
            {
                await RaiseFailureEventAsync(client.ClientId, "Invalid client secret");
                SecretLog.ClientSecretValidationFailed(_logger, client.ClientId);

                return fail;
            }
        }

        SecretLog.ClientValidationSuccess(_logger);

        var success = new ClientSecretValidationResult
        {
            IsError = false,
            Client = client,
            Secret = parsedSecret,
            Confirmation = secretValidationResult?.Confirmation
        };

        await RaiseSuccessEventAsync(client.ClientId, parsedSecret.Type);
        return success;
    }

    private Task RaiseSuccessEventAsync(string clientId, string authMethod)
    {
        return _events.RaiseAsync(new ClientAuthenticationSuccessEvent(clientId, authMethod));
    }

    private Task RaiseFailureEventAsync(string clientId, string message)
    {
        return _events.RaiseAsync(new ClientAuthenticationFailureEvent(clientId, message));
    }
}
