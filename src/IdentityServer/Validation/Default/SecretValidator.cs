namespace IdentityServer.Validation;

/// <summary>
/// Validates secrets using the registered validators
/// </summary>
public class SecretValidator : ISecretsListValidator
{
    private readonly ILogger _logger;
    private readonly IEnumerable<ISecretValidator> _validators;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretValidator"/> class.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <param name="validators">The validators.</param>
    /// <param name="logger">The logger.</param>
    public SecretValidator(TimeProvider clock, IEnumerable<ISecretValidator> validators, ILogger<ISecretsListValidator> logger)
    {
        _clock = clock;
        _validators = validators;
        _logger = logger;
    }

    /// <summary>
    /// Validates the secret.
    /// </summary>
    /// <param name="parsedSecret">The parsed secret.</param>
    /// <param name="secrets">The secrets.</param>
    /// <returns></returns>
    public async Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        var secretsArray = secrets as Secret[] ?? secrets.ToArray();

        var expiredSecrets = secretsArray.Where(s => s.Expiration.HasExpired(_clock.GetUtcNow().UtcDateTime)).ToList();
        if (expiredSecrets.Count > 0)
        {
            expiredSecrets.ForEach(
                ex => SecretLog.SecretExpired(_logger, ex.Description ?? "no description"));
        }

        var currentSecrets = secretsArray.Where(s => !s.Expiration.HasExpired(_clock.GetUtcNow().UtcDateTime)).ToArray();

        foreach (var validator in _validators)
        {
            var secretValidationResult = await validator.ValidateAsync(currentSecrets, parsedSecret);

            if (secretValidationResult.Success)
            {
                SecretLog.SecretValidatorSuccess(_logger, validator.GetType().Name);
                return secretValidationResult;
            }
        }

        SecretLog.SecretValidatorsFailed(_logger);
        return new SecretValidationResult { Success = false };
    }
}
