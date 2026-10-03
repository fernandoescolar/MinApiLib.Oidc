namespace MinApiLib.Oidc.Server.Core.ClientAuthentication;

/// <summary>
/// Validator for an X.509 certificate based client secret using the thumbprint
/// </summary>
public class X509ThumbprintSecretValidator : ISecretValidator
{
    private readonly ILogger<X509ThumbprintSecretValidator> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public X509ThumbprintSecretValidator(ILogger<X509ThumbprintSecretValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<SecretValidationResult> ValidateAsync(IEnumerable<Secret> secrets, ParsedSecret parsedSecret)
    {
        var fail = Task.FromResult(new SecretValidationResult { Success = false });

        if (parsedSecret.Type != ParsedSecretTypes.X509Certificate)
        {
            SecretLog.X509ThumbprintCannotProcess(_logger, parsedSecret.Type ?? "null");
            return fail;
        }

        if (!(parsedSecret.Credential is X509Certificate2 cert))
        {
            throw new InvalidOperationException("Credential is not a x509 certificate.");
        }

        var thumbprint = cert.Thumbprint;
        if (thumbprint == null)
        {
            SecretLog.NoX509Thumbprint(_logger);
            return fail;
        }

        var thumbprintSecrets = secrets.Where(s => s.Type == SecretTypes.X509CertificateThumbprint).ToList();
        if (thumbprintSecrets.Count == 0)
        {
            SecretLog.NoX509ThumbprintSecretsConfigured(_logger);
            return fail;
        }

        // Secrets may hold the SHA-1 thumbprint (40 hex chars) or, preferably, the SHA-256 one (64 hex chars).
        var sha256Thumbprint = cert.GetCertHashString(HashAlgorithmName.SHA256);

        foreach (var thumbprintSecret in thumbprintSecrets)
        {
            if (thumbprint.Equals(thumbprintSecret.Value, StringComparison.OrdinalIgnoreCase) ||
                sha256Thumbprint.Equals(thumbprintSecret.Value, StringComparison.OrdinalIgnoreCase))
            {
                var result = new SecretValidationResult
                {
                    Success = true,
                    Confirmation = cert.CreateThumbprintCnf()
                };

                return Task.FromResult(result);
            }
        }

        SecretLog.NoMatchingX509ThumbprintSecret(_logger);
        return fail;
    }
}
