namespace MinApiLib.OidcServer.Validation;

/// <summary>
/// Validates a secret stored in plain text
/// </summary>
public class PlainTextSharedSecretValidator : ISecretValidator
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlainTextSharedSecretValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PlainTextSharedSecretValidator(ILogger<PlainTextSharedSecretValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a secret
    /// </summary>
    /// <param name="secrets">The stored secrets.</param>
    /// <param name="parsedSecret">The received secret.</param>
    /// <returns>
    /// A validation result
    /// </returns>
    /// <exception cref="System.ArgumentException">id or credential is missing.</exception>
    public Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        var fail = Task.FromResult(new SecretValidationResult { Success = false });
        var success = Task.FromResult(new SecretValidationResult { Success = true });

        if (parsedSecret.Type != OidcServerConstants.ParsedSecretTypes.SharedSecret)
        {
            SecretLog.InvalidParsedSecretType(_logger, parsedSecret.Type ?? "null");
            return fail;
        }

        var sharedSecrets = secrets.Where(s => s.Type == OidcServerConstants.SecretTypes.SharedSecret);
        if (!sharedSecrets.Any())
        {
            SecretLog.NoSharedSecretConfigured(_logger);
            return fail;
        }

        var sharedSecret = parsedSecret.Credential as string;

        if (parsedSecret.Id.IsMissing() || sharedSecret.IsMissing())
        {
            throw new ArgumentException("Id or Credential is missing.");
        }

        foreach (var secret in sharedSecrets)
        {
            var isValid = FixedTimeComparer.IsEqual(sharedSecret, secret.Value);

            if (isValid)
            {
                return success;
            }
        }

        SecretLog.NoMatchingPlainTextSecret(_logger);
        return fail;
    }
}
