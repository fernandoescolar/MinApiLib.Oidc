namespace IdentityServer.Validation;

/// <summary>
/// Validator for an X.509 certificate based client secret using the common name
/// </summary>
public class X509NameSecretValidator : ISecretValidator
{
    private readonly ILogger<X509NameSecretValidator> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public X509NameSecretValidator(ILogger<X509NameSecretValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        var fail = Task.FromResult(new SecretValidationResult { Success = false });

        if (parsedSecret.Type != ParsedSecretTypes.X509Certificate)
        {
            SecretLog.X509NameCannotProcess(_logger, parsedSecret.Type ?? "null");
            return fail;
        }

        if (!(parsedSecret.Credential is X509Certificate2 cert))
        {
            throw new InvalidOperationException("Credential is not a x509 certificate.");
        }

        var name = cert.Subject;
        if (name == null)
        {
            SecretLog.NoX509SubjectName(_logger);
            return fail;
        }

        var nameSecrets = secrets.Where(s => s.Type == SecretTypes.X509CertificateName).ToList();
        if (nameSecrets.Count == 0)
        {
            SecretLog.NoX509NameSecretsConfigured(_logger);
            return fail;
        }

        foreach (var nameSecret in nameSecrets)
        {
            if (name.Equals(nameSecret.Value, StringComparison.Ordinal))
            {
                var result = new SecretValidationResult
                {
                    Success = true,
                    Confirmation = cert.CreateThumbprintCnf()
                };

                return Task.FromResult(result);
            }
        }

        SecretLog.NoMatchingX509NameSecret(_logger);
        return fail;
    }
}
